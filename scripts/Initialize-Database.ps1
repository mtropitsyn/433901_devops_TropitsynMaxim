param([ValidateRange(1,99)][int]$StudentNumber = 16, [ValidateRange(1024,65535)][int]$AppPort = 5316)
$ErrorActionPreference = 'Stop'
$envPath = Join-Path (Split-Path -Parent $PSScriptRoot) '.env'
if (Test-Path -LiteralPath $envPath) {
    Write-Host '.env already exists; existing database credentials are preserved.'
    return
}
function New-DatabasePassword {
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
    [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
}
$lines = @(
    'COMPOSE_PROJECT_NAME=tropitsyn16'
    "STUDENT_NUMBER=$StudentNumber"
    "APP_PORT=$AppPort"
    'DB_PORT=53316'
    "MARIADB_PASSWORD=$(New-DatabasePassword)"
    "MARIADB_ROOT_PASSWORD=$(New-DatabasePassword)"
)
# ASCII avoids a BOM in .env, including in Windows PowerShell 5.1.
[IO.File]::WriteAllLines($envPath, $lines, [Text.Encoding]::ASCII)
Write-Host 'Created .env with random MariaDB passwords. It is excluded from Git and Docker build context.'
