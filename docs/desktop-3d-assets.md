# Desktop 3D Asset Sources

The desktop client uses externally sourced, production-quality 3D models for splash and starmap rendering.

## Current Model Catalog

| Gameplay Object | Local Asset Path | Source URL | Attribution |
| --- | --- | --- | --- |
| Splash Ship | `src/Desktop/Assets/Models/dart_spacecraft.stl` | https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Printing/Double%20Asteroid%20Redirection%20Test%20(DART)/Double%20Asteroid%20Redirection%20Test%20(DART).stl | NASA 3D Resources (public domain U.S. Government work) |
| Starmap Body | `src/Desktop/Assets/Models/rq36_asteroid.glb` | https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Models/1999%20RQ36%20asteroid/1999%20RQ36%20asteroid.glb | NASA 3D Resources (public domain U.S. Government work) |

## Refresh Workflow

Run the sync script to re-download assets from the source URLs:

```powershell
pwsh ./scripts/sync-desktop-model-assets.ps1
```

Use `-Force` to overwrite existing files:

```powershell
pwsh ./scripts/sync-desktop-model-assets.ps1 -Force
```

## Runtime Behavior

- Assets are loaded at runtime via `AssimpNet`.
- Imported models are normalized to consistent unit bounds, then transformed for scene placement.
- If any import fails or assets are unavailable, desktop rendering falls back to procedural geometry.
