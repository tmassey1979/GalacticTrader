param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

$assets = @(
    @{
        Name = "Splash Ship"
        RelativePath = "src/Desktop/Assets/Models/dart_spacecraft.stl"
        Url = "https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Printing/Double%20Asteroid%20Redirection%20Test%20(DART)/Double%20Asteroid%20Redirection%20Test%20(DART).stl"
    },
    @{
        Name = "Starmap Body"
        RelativePath = "src/Desktop/Assets/Models/rq36_asteroid.glb"
        Url = "https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Models/1999%20RQ36%20asteroid/1999%20RQ36%20asteroid.glb"
    }
)

foreach ($asset in $assets) {
    $targetPath = Join-Path $repoRoot $asset.RelativePath
    $targetDirectory = Split-Path -Parent $targetPath
    New-Item -Path $targetDirectory -ItemType Directory -Force | Out-Null

    if ((Test-Path $targetPath) -and -not $Force) {
        Write-Host "Skipping existing asset: $($asset.Name) -> $targetPath"
        continue
    }

    Write-Host "Downloading $($asset.Name) from $($asset.Url)"
    Invoke-WebRequest -Uri $asset.Url -OutFile $targetPath
    Write-Host "Saved: $targetPath"
}
