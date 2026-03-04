# Unity CI/CD

This document covers Unity build and packaging automation in GitHub Actions.

## Workflow

- `.github/workflows/unity-client-build.yml`

## Outputs

- Raw Unity Windows build artifact: `unity-windows-build`
- Windows zip package artifact: `unity-windows-zip`
- Windows installer artifact: `unity-windows-installer`

## Required Secrets

- `UNITY_LICENSE`
- `UNITY_EMAIL` (optional depending on licensing strategy)
- `UNITY_PASSWORD` (optional depending on licensing strategy)

## Project Detection Guard

Build/package jobs run only if a Unity project is present:

- `unity/ProjectSettings/ProjectVersion.txt`
- `unity/Packages/manifest.json`

This prevents failing runs while migration scaffolding is still incomplete.

## Installer Definition

- `infrastructure/unity/installer/GalacticTraderUnity.iss`

The installer is built with Inno Setup in the packaging job and uses run number based versioning for CI outputs.
