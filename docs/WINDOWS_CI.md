# Windows x64 CI build

This repository builds the Unity game in GitHub Actions using GameCI. You do
**not** need Unity Hub or Unity Editor installed on your local PC to run the
workflow. However, Unity still requires an activated license.

## Build configuration

- Unity project: `GameProject/DragonBoy250`
- Unity version: `6000.3.9f1` (from `ProjectSettings/ProjectVersion.txt`)
- Build target: `StandaloneWindows64`
- Editor entry point: `DragonBoy.Build.BuildPlayerCLI.Build`
- Output: `build/StandaloneWindows64/DragonBoy250.exe` with `DragonBoy250_Data/`
- Download: Actions > Build Windows x64 > workflow run > Artifacts >
  `DragonBoy250-Windows-x64`

The workflow builds after changes to the Unity project on `master`, or when
triggered manually using **Actions > Build Windows x64 > Run workflow**.

## Required GitHub Actions secrets

Add these under **Settings > Secrets and variables > Actions**:

| Secret | Purpose |
| --- | --- |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `UNITY_LICENSE` | Unity Personal license file (.ulf) contents |
| `UNITY_SERIAL` | Unity Pro serial (instead of UNITY_LICENSE) |

Set `UNITY_EMAIL` and `UNITY_PASSWORD` plus *one* of `UNITY_LICENSE` or
`UNITY_SERIAL`. Never commit a license, password, or serial to the repository.

For Unity Personal, the officially documented GameCI process is to activate
through Unity Hub once and copy the resulting .ulf license content. If you
have no Unity Hub and no activated license, the workflow cannot perform a
licensed Unity build; it will fail early with a clear setup message. Unity
Hub is not required on the GitHub Actions runner.

GameCI license setup: https://game.ci/docs/github/activation/

## Troubleshooting

- **License validation failed:** Configure the secrets above. A GitHub Actions
  workflow cannot bypass Unity license activation.
- **Editor/image download failed:** Check GameCI's availability for the exact
  Unity version. Do not silently change the project Unity version as a
  workaround.
- **Unity compilation error:** Inspect the Build Windows x64 job logs. This
  workflow runs the game's existing Editor build method and compiles its
  enabled scenes. There may be code or platform-specific errors to resolve.
- **Missing executable:** Unity must emit both `DragonBoy250.exe` and
  `DragonBoy250_Data/` into `build/StandaloneWindows64/`; the job rejects an
  incomplete artifact.

The workflow compiles a Windows *Mono* standalone player in GameCI's Linux
environment. If the project's standalone scripting backend changes to IL2CPP,
a compatible Windows toolchain/runner strategy will be needed.

The workflow was added without running an actual licensed Unity build. A
successful GitHub Actions run is required before treating it as verified.
