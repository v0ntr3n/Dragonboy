import SwiftUI

struct AccountRowView: View {
    @EnvironmentObject private var store: AccountStore
    let account: Account
    let onLaunchOrStop: () -> Void

    private var displayName: String {
        let characterName = account.characterInfo?.cName ?? ""
        return characterName.isEmpty ? account.username : characterName
    }

    private var subtitleText: String {
        if let mapName = account.characterInfo?.mapName, !mapName.isEmpty {
            return mapName
        }
        return account.server
    }

    var body: some View {
        HStack(spacing: 10) {
            Circle()
                .fill(account.connectionStatus.isConnected ? .green : .secondary.opacity(0.35))
                .frame(width: 32, height: 32)
                .overlay {
                    Text(displayName.prefix(1).uppercased())
                        .font(.caption.weight(.bold))
                        .foregroundStyle(.white)
                }

            VStack(alignment: .leading, spacing: 2) {
                Text(displayName)
                    .font(.body.weight(.medium))
                    .lineLimit(1)

                Text(subtitleText)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            }

            Spacer(minLength: 8)

            if store.launching.contains(account.id) {
                ProgressView()
                    .controlSize(.small)
            } else {
                Button(action: onLaunchOrStop) {
                    Image(systemName: account.connectionStatus.isConnected ? "stop.fill" : "play.fill")
                        .font(.body.weight(.bold))
                        .frame(width: 32, height: 32)
                }
                .buttonStyle(.borderless)
                .foregroundStyle(account.connectionStatus.isConnected ? Color.red : Color.accentColor)
                .help(account.connectionStatus.isConnected ? "Stop game" : "Launch game")
            }
        }
        .padding(.vertical, 4)
        .accessibilityElement(children: .combine)
        .accessibilityLabel("\(displayName), \(account.connectionStatus.displayText.isEmpty ? "offline" : account.connectionStatus.displayText)")
    }
}