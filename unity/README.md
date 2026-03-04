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
