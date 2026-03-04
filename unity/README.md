# Unity Client Workspace

This directory is the target workspace for the Unity-based game client migration from WPF.

## Purpose

- Host the Unity project and game client code.
- Preserve online-only gameplay model against existing GalacticTrader APIs.
- Support CI/CD packaging for both zip and installer outputs.

## Current Status

- Migration bootstrap scaffolding is in place.
- CI pipeline exists and is gated until a valid Unity project is committed.
- Installer definition is versioned under `infrastructure/unity/installer`.
- Shared auth/session lifecycle scaffolding is present under `Assets/Scripts/Auth`:
  - `UnityAuthController`
  - `PlayerPrefsSessionStore`
  - `AuthFailureMessageMapper`
- Shell/navigation scaffolding is present under `Assets/Scripts/Shell`:
  - `UnityModuleHostController`
  - `UnityHotkeyModuleRouter`
  - `UnityShellModule`
  - `ModuleUxStateOverlayController`
- Starmap streaming scaffolding is present under `Assets/Scripts/Starmap`:
  - `UnityStarmapStreamingController`
  - `StarmapDtoMapper`
- Dashboard module scaffolding is present under `Assets/Scripts/Modules/Dashboard`:
  - `UnityDashboardModuleController`
  - realtime strategic snapshot projection into dashboard board/feed state
- Realtime scaffolding is present under `Assets/Scripts/Realtime`:
  - `UnityRealtimeController`

## Target Platforms

- Baseline targets: Windows, Linux, macOS.
- Planned targets (requires platform approvals/tooling): Xbox and PS4.

## Target Structure

- `Assets/`: scenes, scripts, prefabs, art, and gameplay UI.
- `Packages/`: Unity package manifest/lock.
- `ProjectSettings/`: Unity project configuration.

## Activation Notes

CI build job auto-skips until both files exist:

- `unity/ProjectSettings/ProjectVersion.txt`
- `unity/Packages/manifest.json`

Once those are present, configure repository secrets for Unity build licensing:

- `UNITY_LICENSE`
- `UNITY_EMAIL` (optional depending on activation model)
- `UNITY_PASSWORD` (optional depending on activation model)
