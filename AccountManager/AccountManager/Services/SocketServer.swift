import Foundation
import Network

extension Notification.Name {
    static let socketClientConnected    = Notification.Name("socketClientConnected")

    static let socketClientDisconnected = Notification.Name("socketClientDisconnected")

    static let socketMessageReceived    = Notification.Name("socketMessageReceived")
}

private enum EIOPacketType: Int {
    case open    = 0
    case close   = 1
    case ping    = 2
    case pong    = 3
    case message = 4
    case upgrade = 5
    case noop    = 6
}

private enum SIOPacketType: Int {
    case connect      = 0
    case disconnect   = 1
    case event        = 2
    case ack          = 3
    case connectError = 4
    case binaryEvent  = 5
    case binaryAck    = 6
}

final class SocketServer {
    private(set) var port: UInt16 = 0

    let accountID: UUID
    let username: String
    let password: String
    let server: String
    let authToken: String?

    private let pingInterval: Int = 25_000
    private let pingTimeout:  Int = 20_000

    /// Called (on the internal queue) when the last client disconnects
    /// after at least one client had connected.
    var onAllClientsDisconnected: ((UUID) -> Void)?

    private var listener: NWListener?
    private var connections: [NWConnection] = []
    private var authenticatedConnections: Set<ObjectIdentifier> = []
    private var pingTimers: [ObjectIdentifier: DispatchSourceTimer] = [:]
    private var lastPongReceivedAt: [ObjectIdentifier: Date] = [:]
    private let queue: DispatchQueue

    /// Tracks whether at least one WebSocket client has successfully connected.
    private var hasConnectedOnce = false
    private var isStopped = false

    init(
        accountID: UUID,
        username: String,
        password: String,
        server: String = "",
        authToken: String? = nil
    ) {
        self.accountID = accountID
        self.username  = username
        self.password  = password
        self.server    = server
        self.authToken = authToken
        self.queue     = DispatchQueue(label: "socketserver.\(accountID.uuidString)", qos: .userInitiated)
    }

    @discardableResult
    func start(port: NWEndpoint.Port? = nil) throws -> UInt16 {
        dispatchPrecondition(condition: .notOnQueue(.main))

        let params = NWParameters(tls: nil)
        params.allowLocalEndpointReuse = true
        params.requiredLocalEndpoint = NWEndpoint.hostPort(host: .ipv4(.loopback), port: port ?? .any)

        let wsOptions = NWProtocolWebSocket.Options()
        wsOptions.autoReplyPing = true
        params.defaultProtocolStack.applicationProtocols.insert(wsOptions, at: 0)

        let nwListener = try NWListener(using: params)
        self.listener = nwListener

        let semaphore = DispatchSemaphore(value: 0)
        var startError: Error?

        nwListener.stateUpdateHandler = { [weak self] state in
            switch state {
            case .ready:
                if let assignedPort = nwListener.port {
                    self?.port = assignedPort.rawValue
                }
                semaphore.signal()
            case .failed(let error):
                startError = error
                semaphore.signal()
            case .cancelled:
                semaphore.signal()
            default:
                break
            }
        }

        nwListener.newConnectionHandler = { [weak self] connection in
            self?.acceptConnection(connection)
        }

        nwListener.start(queue: queue)

        let timeout = semaphore.wait(timeout: .now() + 5)
        if timeout == .timedOut {
            nwListener.cancel()
            throw NSError(domain: "SocketServer", code: -1,
                          userInfo: [NSLocalizedDescriptionKey:
                                        "Timed out waiting for WebSocket listener"])
        }
        if let err = startError { throw err }

        print("[SocketServer] Listening on 127.0.0.1:\(self.port) for \(username) (Socket.IO EIO=4)")
        return self.port
    }

    func stop() {
        queue.async { [weak self] in
            guard let self = self, !self.isStopped else { return }
            self.isStopped = true

            let activeConnections = self.connections
            self.connections.removeAll()
            self.authenticatedConnections.removeAll()
            self.lastPongReceivedAt.removeAll()

            for (_, timer) in self.pingTimers {
                timer.cancel()
            }
            self.pingTimers.removeAll()

            for conn in activeConnections {
                conn.stateUpdateHandler = nil
                conn.cancel()
            }

            self.listener?.stateUpdateHandler = nil
            self.listener?.newConnectionHandler = nil
            self.listener?.cancel()
            self.listener = nil
            print("[SocketServer] Stopped server on port \(self.port) for \(self.username)")
        }
    }

    func emitEvent(_ event: String, data: Any, to connection: NWConnection) {
        guard let jsonData = try? JSONSerialization.data(withJSONObject: [event, data]),
              let jsonString = String(data: jsonData, encoding: .utf8) else { return }
        sendText("42\(jsonString)", to: connection)
    }

    func broadcastEvent(_ event: String, data: Any) {
        queue.async { [weak self] in
            guard let self = self, !self.isStopped else { return }
            for conn in self.connections {
                self.emitEvent(event, data: data, to: conn)
            }
        }
    }

    private func isAuthorized(
        _ dict: [String: Any]?,
        for connection: NWConnection,
        requireExplicitToken: Bool = false
    ) -> Bool {
        guard let expected = authToken, !expected.isEmpty else {
            return true
        }
        let key = ObjectIdentifier(connection)
        if let provided = dict?["token"] as? String, provided == expected {
            authenticatedConnections.insert(key)
            return true
        }
        if !requireExplicitToken && authenticatedConnections.contains(key) {
            return true
        }
        return false
    }

    private func sendCredentials(to connection: NWConnection) {
        var payload: [String: Any] = [
            "action": "credentials",
            "username": username,
            "password": password
        ]
        if !server.isEmpty {
            payload["server"] = server
        }
        emitEvent("credentials", data: payload, to: connection)
    }

    private func acceptConnection(_ connection: NWConnection) {
        guard !isStopped else {
            connection.cancel()
            return
        }
        connections.append(connection)

        connection.stateUpdateHandler = { [weak self] state in
            guard let self = self else { return }
            switch state {
            case .ready:
                print("[SocketServer:\(self.port)] WebSocket client connected")
                self.hasConnectedOnce = true
                self.lastPongReceivedAt[ObjectIdentifier(connection)] = Date()
                self.performEngineIOHandshake(connection)
                NotificationCenter.default.post(
                    name: .socketClientConnected, object: nil,
                    userInfo: ["accountID": self.accountID, "port": self.port])
            case .failed, .cancelled:
                self.removeConnection(connection)
            default:
                break
            }
        }

        connection.start(queue: queue)
    }

    private func performEngineIOHandshake(_ connection: NWConnection) {
        let sid = generateSID()

        let openPayload: [String: Any] = [
            "sid": sid,
            "upgrades": [],
            "pingInterval": pingInterval,
            "pingTimeout": pingTimeout
        ]
        if let json = try? JSONSerialization.data(withJSONObject: openPayload),
           let jsonStr = String(data: json, encoding: .utf8) {
            sendText("\(EIOPacketType.open.rawValue)\(jsonStr)", to: connection)
        }

        let connectPayload: [String: Any] = ["sid": sid]
        if let json = try? JSONSerialization.data(withJSONObject: connectPayload),
           let jsonStr = String(data: json, encoding: .utf8) {
            sendText("4\(SIOPacketType.connect.rawValue)\(jsonStr)", to: connection)
        }

        receiveLoop(connection)
        startPingTimer(for: connection)
    }

    private func sendText(_ text: String, to connection: NWConnection) {
        let metadata = NWProtocolWebSocket.Metadata(opcode: .text)
        let context  = NWConnection.ContentContext(
            identifier: "socketio",
            metadata: [metadata]
        )
        guard let data = text.data(using: .utf8) else { return }
        connection.send(content: data, contentContext: context,
                        isComplete: true,
                        completion: .contentProcessed { error in
            if let error = error {
                print("[SocketServer:\(self.port)] WS send error: \(error)")
            }
        })
    }

    private func receiveLoop(_ connection: NWConnection) {
        connection.receiveMessage { [weak self] data, context, isComplete, error in
            guard let self = self else { return }

            if let wsMeta = context?.protocolMetadata(definition: NWProtocolWebSocket.definition) as? NWProtocolWebSocket.Metadata,
               wsMeta.opcode == .close {
                self.removeConnection(connection)
                return
            }

            if let data = data, !data.isEmpty,
               let text = String(data: data, encoding: .utf8) {
                self.handlePacket(text, from: connection)
            }

            if let error = error {
                print("[SocketServer:\(self.port)] WS receive error: \(error)")
                self.removeConnection(connection)
            } else if context?.isFinal == true && (data == nil || data?.isEmpty == true) {
                self.removeConnection(connection)
            } else if !self.isStopped && self.connections.contains(where: { $0 === connection }) {
                self.receiveLoop(connection)
            }
        }
    }

    private func handlePacket(_ packet: String, from connection: NWConnection) {
        let trimmed = packet.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let firstChar = trimmed.first else { return }

        if trimmed == "close-socket" {
            guard isAuthorized(nil, for: connection) else {
                print("[SocketServer:\(port)] Rejected unauthorized raw close-socket")
                return
            }
            removeConnection(connection)
            return
        }

        if trimmed == "3" || (firstChar == "3" && trimmed.dropFirst().allSatisfy({ !$0.isNumber })) {
            lastPongReceivedAt[ObjectIdentifier(connection)] = Date()
            return
        }

        if let eioType = EIOPacketType(rawValue: Int(String(firstChar)) ?? -1) {
            switch eioType {
            case .ping:
                let body = String(trimmed.dropFirst())
                sendText("\(EIOPacketType.pong.rawValue)\(body)", to: connection)

            case .pong:
                lastPongReceivedAt[ObjectIdentifier(connection)] = Date()

            case .message:
                let sioPayload = String(trimmed.dropFirst())
                handleSIOPacket(sioPayload, from: connection)

            case .close:
                removeConnection(connection)

            default:
                break
            }
            return
        }

        if firstChar == "{" || firstChar == "[" {
            handleRawJSON(trimmed, from: connection)
            return
        }

        print("[SocketServer:\(port)] Unknown packet: \(trimmed.prefix(80))")
    }

    private func handleRawJSON(_ text: String, from connection: NWConnection) {
        guard let data = text.data(using: .utf8),
              let dict = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            print("[SocketServer:\(port)] Failed to parse raw JSON: \(text.prefix(80))")
            return
        }

        let action = dict["action"] as? String ?? "unknown"
        let requireToken = (action == "get_credentials")

        guard isAuthorized(dict, for: connection, requireExplicitToken: requireToken) else {
            print("[SocketServer:\(port)] Rejected unauthorized raw JSON action '\(action)'")
            if requireToken {
                removeConnection(connection)
            }
            return
        }

        switch action {
        case "get_credentials":
            sendCredentials(to: connection)
            return

        case "close-socket":
            removeConnection(connection)
            return

        default:
            break
        }

        NotificationCenter.default.post(
            name: .socketMessageReceived, object: nil,
            userInfo: ["accountID": accountID,
                       "event": action,
                       "args": [dict]])
    }

    private func handleSIOPacket(_ payload: String, from connection: NWConnection) {
        guard let firstChar = payload.first,
              let sioType = SIOPacketType(rawValue: Int(String(firstChar)) ?? -1) else {
            return
        }

        switch sioType {
        case .connect:
            let sid = generateSID()
            let ack: [String: Any] = ["sid": sid]
            if let json = try? JSONSerialization.data(withJSONObject: ack),
               let jsonStr = String(data: json, encoding: .utf8) {
                sendText("4\(SIOPacketType.connect.rawValue)\(jsonStr)", to: connection)
            }

        case .event:
            let jsonPart = String(payload.dropFirst())
            handleEvent(jsonPart, from: connection)

        case .disconnect:
            removeConnection(connection)

        default:
            break
        }
    }

    private func handleEvent(_ json: String, from connection: NWConnection) {
        guard let data = json.data(using: .utf8),
              let array = try? JSONSerialization.jsonObject(with: data) as? [Any],
              let eventName = array.first as? String else { return }

        let args = Array(array.dropFirst())
        let firstDict = args.first as? [String: Any]
        let requireToken = (eventName == "get_credentials")

        guard isAuthorized(firstDict, for: connection, requireExplicitToken: requireToken) else {
            print("[SocketServer:\(port)] Rejected unauthorized event '\(eventName)'")
            if requireToken {
                removeConnection(connection)
            }
            return
        }

        print("[SocketServer:\(port)] Event '\(eventName)'")

        switch eventName {
        case "get_credentials":
            sendCredentials(to: connection)
            return

        case "close-socket":
            removeConnection(connection)
            return

        case "ping":
            emitEvent("pong", data: [:], to: connection)

        default:
            break
        }

        NotificationCenter.default.post(
            name: .socketMessageReceived, object: nil,
            userInfo: ["accountID": accountID,
                       "event": eventName,
                       "args": args])
    }

    private func startPingTimer(for connection: NWConnection) {
        let key = ObjectIdentifier(connection)
        pingTimers[key]?.cancel()
        let timer = DispatchSource.makeTimerSource(queue: queue)
        timer.schedule(deadline: .now() + .milliseconds(pingInterval),
                       repeating: .milliseconds(pingInterval))
        timer.setEventHandler { [weak self] in
            self?.sendText("\(EIOPacketType.ping.rawValue)", to: connection)
        }
        timer.resume()
        pingTimers[key] = timer
    }

    private func generateSID() -> String {
        UUID().uuidString
            .replacingOccurrences(of: "-", with: "")
            .prefix(20)
            .lowercased()
    }

    private func removeConnection(_ connection: NWConnection) {
        guard let idx = connections.firstIndex(where: { $0 === connection }) else { return }
        connections.remove(at: idx)

        let key = ObjectIdentifier(connection)
        authenticatedConnections.remove(key)
        lastPongReceivedAt.removeValue(forKey: key)
        pingTimers[key]?.cancel()
        pingTimers.removeValue(forKey: key)

        connection.stateUpdateHandler = nil
        connection.cancel()

        guard !isStopped else { return }

        print("[SocketServer:\(port)] Client disconnected")
        NotificationCenter.default.post(
            name: .socketClientDisconnected, object: nil,
            userInfo: ["accountID": accountID, "port": port])

        if connections.isEmpty && hasConnectedOnce {
            print("[SocketServer:\(port)] All clients disconnected – triggering process cleanup")
            onAllClientsDisconnected?(accountID)
        }
    }
}