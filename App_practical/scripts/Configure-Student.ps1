param([Parameter(Mandatory=$true)][ValidateRange(1,99)][int]$StudentNumber)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$port = 5000 + $StudentNumber
$settingsPath = Join-Path $projectRoot 'CalculatorWeb\appsettings.json'
$launchPath = Join-Path $projectRoot 'CalculatorWeb\Properties\launchSettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$settings.Kestrel.Endpoints.Http.Url = "http://0.0.0.0:$port"
$settings | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $settingsPath -Encoding utf8
$launch = Get-Content -LiteralPath $launchPath -Raw | ConvertFrom-Json
$launch.profiles.CalculatorWeb.applicationUrl = "http://localhost:$port"
$launch | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $launchPath -Encoding utf8
Write-Host "Configured student $StudentNumber; URL: http://localhost:$port"
