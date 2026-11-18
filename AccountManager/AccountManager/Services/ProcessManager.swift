import Foundation
import AppKit
import Darwin

final class ProcessManager {
    static let shared = ProcessManager()

    weak var store: AccountStore?

    private let stopGraceTimeout: TimeInterval = 2.0

    /// All dictionaries below are mutated/read exclusively on the main queue.
    private var socketServers: [UUID: SocketServer] = [:]
    private var processMonitors: [UUID: DispatchSourceProcess] = [:]
    private var launchedPIDs: [UUID: pid_t] = [:]
    private var stopKillTimers: [UUID: DispatchWorkItem] = [:]

    private func onMainSync<T>(_ work: () -> T) -> T {
        if Thread.isMainThread {
            return work()
        } else {
            return DispatchQueue.main.sync(execute: work)
        }
    }

    private static func processExists(_ pid: pid_t) -> Bool {
        guard pid > 0 else { return false }
        errno = 0
        if kill(pid, 0) == 0 {
            return true
        }
        return errno != ESRCH
    }

    func launch(account: Account) throws {
        let accountID = account.id
        let ipcToken = UUID().uuidString

        let server = SocketServer(
            accountID: accountID,
            username: account.username,
            password: account.password,
            server: account.server,
            authToken: ipcToken
        )

        server.onAllClientsDisconnected = { [weak self] disconnectedAccountID in
            self?.handleSocketDisconnected(accountID: disconnectedAccountID)
        }

        let port = try server.start()

        onMainSync {
            self.cleanupAllOnMain(for: accountID)
            self.socketServers[accountID] = server
        }

        var args: [String] = [
            "-port", String(port),
            "-username", account.username,
            "-ipcToken", ipcToken
        ]
        if !account.server.isEmpty {
            args.append("-server")
            args.append(account.server)
        }

        let pid: pid_t
        do {
            pid = try Launcher.launch(bundlePath: Launcher.defaultBundlePath, args: args)
        } catch {
            onMainSync {
                if self.socketServers[accountID] === server {
                    self.cleanupAllOnMain(for: accountID)
                } else {
                    server.stop()
                }
                self.store?.launching.remove(accountID)
            }
            throw error
        }

        onMainSync {
            guard self.socketServers[accountID] === server else {
                // Account was stopped or relaunched concurrently while NSWorkspace was opening.
                if Self.processExists(pid) {
                    _ = kill(pid, SIGTERM)
                }
                return
            }
            self.monitorProcessOnMain(pid: pid, for: accountID)
        }
    }

    private func monitorProcessOnMain(pid: pid_t, for accountID: UUID) {
        dispatchPrecondition(condition: .onQueue(.main))

        processMonitors[accountID]?.cancel()
        processMonitors.removeValue(forKey: accountID)
        launchedPIDs[accountID] = pid

        let source = DispatchSource.makeProcessSource(identifier: pid, eventMask: .exit, queue: .main)
        source.setEventHandler { [weak self] in
            guard let self = self else { return }
            guard self.launchedPIDs[accountID] == pid else { return }
            print("[ProcessManager] Process \(pid) exited for \(accountID) – cleaning up")
            self.cleanupAllOnMain(for: accountID)
            self.store?.markStopped(accountID: accountID)
        }
        source.resume()
        processMonitors[accountID] = source

        if !Self.processExists(pid) {
            print("[ProcessManager] Process \(pid) already exited for \(accountID) – cleaning up")
            cleanupAllOnMain(for: accountID)
            store?.markStopped(accountID: accountID)
        }
    }

    func stop(account: Account) {
        let accountID = account.id

        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }

            let server = self.socketServers[accountID]
            let pid = self.launchedPIDs[accountID]

            if let server = server {
                server.broadcastEvent("stop", data: ["action": "stop"])
                print("[ProcessManager] Sent 'stop' command to game for \(accountID)")

                self.stopKillTimers[accountID]?.cancel()
                let timer = DispatchWorkItem { [weak self] in
                    guard let self = self else { return }
                    self.stopKillTimers.removeValue(forKey: accountID)

                    if let trackedPid = self.launchedPIDs[accountID],
                       trackedPid > 0,
                       Self.processExists(trackedPid) {
                        print("[ProcessManager] Client unresponsive for \(accountID); sending SIGTERM to pid \(trackedPid)")
                        _ = kill(trackedPid, SIGTERM)
                    }

                    if let activeServer = self.socketServers.removeValue(forKey: accountID) {
                        activeServer.stop()
                    }
                    self.store?.markStopped(accountID: accountID)
                }
                self.stopKillTimers[accountID] = timer
                DispatchQueue.main.asyncAfter(deadline: .now() + self.stopGraceTimeout, execute: timer)
            } else if let pid = pid, pid > 0, Self.processExists(pid) {
                print("[ProcessManager] Sending SIGTERM to pid \(pid) for \(accountID)")
                _ = kill(pid, SIGTERM)
                self.cleanupAllOnMain(for: accountID)
                self.store?.markStopped(accountID: accountID)
            } else {
                print("[ProcessManager] No active server or process for \(accountID) – marking stopped")
                self.cleanupAllOnMain(for: accountID)
                self.store?.markStopped(accountID: accountID)
            }
        }
    }

    private func handleSocketDisconnected(accountID: UUID) {
        print("[ProcessManager] Socket disconnected for \(accountID) – cleaning up")

        DispatchQueue.main.async { [weak self] in
            guard let self = self else { return }
            if let server = self.socketServers.removeValue(forKey: accountID) {
                server.stop()
            }
            self.store?.markStopped(accountID: accountID)
        }
    }

    private func cleanupAllOnMain(for accountID: UUID) {
        dispatchPrecondition(condition: .onQueue(.main))

        stopKillTimers[accountID]?.cancel()
        stopKillTimers.removeValue(forKey: accountID)

        processMonitors[accountID]?.cancel()
        processMonitors.removeValue(forKey: accountID)

        launchedPIDs.removeValue(forKey: accountID)

        if let server = socketServers.removeValue(forKey: accountID) {
            server.stop()
        }
    }

    func socketServer(for accountID: UUID) -> SocketServer? {
        onMainSync {
            socketServers[accountID]
        }
    }
}