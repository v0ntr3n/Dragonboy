import Foundation
import AppKit
import Darwin

enum LauncherError: Error {
    case appNotFound(path: String)
    case executableNotFound(path: String)
    case launchFailed(Error)
}

extension LauncherError: LocalizedError {
    var errorDescription: String? {
        switch self {
        case .appNotFound(let path):
            return "Application bundle not found at path: \(path)"
        case .executableNotFound(let path):
            return "Could not find an executable inside the app bundle at: \(path)"
        case .launchFailed(let err):
            return "Failed to launch process: \(err.localizedDescription)"
        }
    }
}

final class Launcher {
    /// Default bundle path used when none provided
    static let defaultBundlePath = "/Applications/Dragonboy250.app"

    static func isAppInstalled(at bundlePath: String) -> Bool {
        return FileManager.default.fileExists(atPath: bundlePath)
    }

    @discardableResult
    static func launch(bundlePath: String, args: [String] = []) throws -> pid_t {
        dispatchPrecondition(condition: .notOnQueue(.main))

        guard isAppInstalled(at: bundlePath) else {
            throw LauncherError.appNotFound(path: bundlePath)
        }

        let appURL = URL(fileURLWithPath: bundlePath)
        let config = NSWorkspace.OpenConfiguration()
        config.arguments = args
        config.createsNewApplicationInstance = true

        var resultApp: NSRunningApplication?
        var resultError: Error?
        let semaphore = DispatchSemaphore(value: 0)

        NSWorkspace.shared.openApplication(at: appURL, configuration: config) { app, error in
            resultApp = app
            resultError = error
            semaphore.signal()
        }

        let timeout = semaphore.wait(timeout: .now() + 30)

        if timeout == .timedOut {
            throw LauncherError.launchFailed(NSError(domain: "Launcher", code: -2, userInfo: [
                NSLocalizedDescriptionKey: "Timed out waiting for app to launch"
            ]))
        }

        if let error = resultError {
            throw LauncherError.launchFailed(error)
        }

        guard let app = resultApp else {
            throw LauncherError.launchFailed(NSError(domain: "Launcher", code: -1, userInfo: [
                NSLocalizedDescriptionKey: "No running application returned"
            ]))
        }

        let pid = app.processIdentifier
        if pid <= 0 {
            throw LauncherError.launchFailed(NSError(domain: "Launcher", code: -1, userInfo: [
                NSLocalizedDescriptionKey: "Invalid PID returned: \(pid)"
            ]))
        }

        return pid
    }
}