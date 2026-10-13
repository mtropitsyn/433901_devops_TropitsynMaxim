$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Initialize-Database.ps1')
$settings = @{}
foreach ($line in Get-Content -LiteralPath (Join-Path $projectRoot '.env')) {
    if ($line -match '^([A-Z_]+)=(.*)$') { $settings[$Matches[1]] = $Matches[2] }
}
Push-Location $projectRoot
$previousPassword = $env:Database__Password
$previousPort = $env:Database__Port
try {
    & docker compose up -d --wait db
    if ($LASTEXITCODE -ne 0) { throw 'MariaDB did not start.' }
    $env:Database__Password = $settings['MARIADB_PASSWORD']
    $env:Database__Port = $settings['DB_PORT']
    & dotnet run --project CalculatorWeb
    if ($LASTEXITCODE -ne 0) { throw 'The local application failed.' }
} finally {
    $env:Database__Password = $previousPassword
    $env:Database__Port = $previousPort
    Pop-Location
}
