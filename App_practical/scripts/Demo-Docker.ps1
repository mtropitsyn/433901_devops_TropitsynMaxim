param([Parameter(Mandatory=$true)][ValidateRange(1,99)][int]$StudentNumber)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$port = 5000 + $StudentNumber
$imageName = "${StudentNumber}-calculator:latest"
$containerName = "calculator-$StudentNumber"
function Docker-Step([string[]]$DockerArguments) {
    Write-Host "docker $($DockerArguments -join ' ')"
    & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($DockerArguments -join ' ')" }
}
function Wait-App {
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        try { $r=Invoke-WebRequest -Uri "http://localhost:$port/health" -UseBasicParsing -TimeoutSec 2; if ($r.Content -eq 'OK') { return } } catch { }
        Start-Sleep -Seconds 1
    }
    throw 'The container did not become ready within 30 attempts.'
}
Push-Location $projectRoot
try {
    Docker-Step @('version')
    Docker-Step @('build','--build-arg',"APP_PORT=$port",'-t',$imageName,'.')
    Docker-Step @('images','-a',$imageName)
    # Existing containers are preserved; a name conflict is reported by Docker.
    Docker-Step @('run','-d','-p',"${port}:${port}",'--name',$containerName,$imageName)
    try {
        Wait-App
        Docker-Step @('ps','--filter',"name=$containerName")
        & (Join-Path $PSScriptRoot 'Test-Web.ps1') -BaseUrl "http://localhost:$port"
        Docker-Step @('logs',$containerName)
        Docker-Step @('stop',$containerName)
        Docker-Step @('inspect','--format','{{.State.Status}}',$containerName)
        $state = & docker inspect --format '{{.State.Status}}' $containerName
        if ($LASTEXITCODE -ne 0 -or $state.Trim() -ne 'exited') { throw 'The container was not stopped.' }
        $accessible=$false
        try { Invoke-WebRequest -Uri "http://localhost:$port/health" -UseBasicParsing -TimeoutSec 2 | Out-Null; $accessible=$true } catch { }
        if ($accessible) { throw 'The application is still accessible after docker stop.' }
        Write-Host 'PASS: the stopped application is inaccessible'
        Docker-Step @('start',$containerName)
        Wait-App
        Write-Host 'PASS: the application responds again after docker start'
    } finally {
        Docker-Step @('stop',$containerName)
        Docker-Step @('ps','-a','--filter',"name=$containerName")
    }
    Write-Host "Demonstration completed. The container remains stopped. Use: docker start $containerName"
} finally { Pop-Location }
