$ErrorActionPreference = "Stop"

$gatewayUrl = if ($env:GATEWAY_URL) { $env:GATEWAY_URL.TrimEnd("/") } else { "http://localhost:8081" }

function Assert-StatusCode {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][int]$ExpectedStatusCode,
        [string]$Body
    )

    $uri = "$gatewayUrl$Path"
    try {
        if ($Method -ieq "GET" -or [string]::IsNullOrWhiteSpace($Body)) {
            $response = Invoke-WebRequest -Uri $uri -Method $Method -UseBasicParsing
        }
        else {
            $response = Invoke-WebRequest -Uri $uri -Method $Method -Body $Body -ContentType "application/json" -UseBasicParsing
        }
        $statusCode = [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }

    if ($statusCode -ne $ExpectedStatusCode) {
        throw "Expected $Method $Path => $ExpectedStatusCode, got $statusCode"
    }

    Write-Host "OK $Method $Path => $statusCode"
}

Assert-StatusCode -Method "GET" -Path "/health/live" -ExpectedStatusCode 200
Assert-StatusCode -Method "POST" -Path "/api/auth/register" -ExpectedStatusCode 400 -Body "{}"
Assert-StatusCode -Method "GET" -Path "/api/navigation/sectors" -ExpectedStatusCode 401
Assert-StatusCode -Method "GET" -Path "/metrics" -ExpectedStatusCode 200

Write-Host "Gateway smoke checks passed."
