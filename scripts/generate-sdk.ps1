param(
    [string]$ApiBaseUrl = "http://localhost:8080",
    [string]$OutputDir = "generated-clients"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$specPath = Join-Path $OutputDir "openapi.json"

Write-Host "Downloading OpenAPI document from $ApiBaseUrl/swagger/v1/swagger.json"
Invoke-WebRequest -Uri "$ApiBaseUrl/swagger/v1/swagger.json" -OutFile $specPath

Write-Host "Generating TypeScript SDK"
npx @openapitools/openapi-generator-cli generate -i $specPath -g typescript-fetch -o (Join-Path $OutputDir "typescript")

Write-Host "Generating C# SDK"
npx @openapitools/openapi-generator-cli generate -i $specPath -g csharp -o (Join-Path $OutputDir "csharp")

Write-Host "SDK generation complete in $OutputDir"
