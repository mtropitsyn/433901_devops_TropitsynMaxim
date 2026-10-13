param([ValidateRange(1,99)][int]$StudentNumber = 16)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$port = 5000 + $StudentNumber
function Compose-Step([string[]]$ComposeArguments) {
    Write-Host "docker compose $($ComposeArguments -join ' ')"
    & docker compose @ComposeArguments
    if ($LASTEXITCODE -ne 0) { throw "Compose command failed: $($ComposeArguments -join ' ')" }
}
function Wait-App {
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        try { $r=Invoke-WebRequest -Uri "http://localhost:$port/health/ready" -UseBasicParsing -TimeoutSec 8; if ($r.Content -eq 'OK') { return } } catch { }
        Start-Sleep -Seconds 1
    }
    throw 'The container did not become ready within 30 attempts.'
}
$previousPort = $env:APP_PORT
$previousNumber = $env:STUDENT_NUMBER
Push-Location $projectRoot
try {
    & (Join-Path $PSScriptRoot 'Initialize-Database.ps1') -StudentNumber $StudentNumber
    $env:APP_PORT = [string]$port
    $env:STUDENT_NUMBER = [string]$StudentNumber
    Compose-Step @('up','-d','--build','--wait')
    try {
        Wait-App
        Compose-Step @('ps')
        & (Join-Path $PSScriptRoot 'Test-Database.ps1') -BaseUrl "http://localhost:$port"
        Compose-Step @('stop','web')
        $state = (& docker compose ps -a --format json web | ConvertFrom-Json).State
        if ($LASTEXITCODE -ne 0 -or $state -ne 'exited') { throw 'The web container was not stopped.' }
        $accessible=$false
        try { Invoke-WebRequest -Uri "http://localhost:$port/health" -UseBasicParsing -TimeoutSec 2 | Out-Null; $accessible=$true } catch { }
        if ($accessible) { throw 'The application is still accessible after docker stop.' }
        Write-Host 'PASS: the stopped application is inaccessible'
        Compose-Step @('start','web')
        Wait-App
        Write-Host 'PASS: the application responds again after docker start'
    } finally {
        Compose-Step @('stop')
        Compose-Step @('ps','-a')
    }
    Write-Host 'Demonstration completed. MariaDB data is preserved. Use: docker compose start'
} finally {
    $env:APP_PORT = $previousPort
    $env:STUDENT_NUMBER = $previousNumber
    Pop-Location
}
