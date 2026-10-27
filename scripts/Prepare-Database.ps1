$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $sql = Get-Content -LiteralPath 'database/prepare.sql' -Raw
    $sql | & docker compose exec -T db sh -c 'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" exec mariadb --user=root'
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare 16-WebCalculatorDb. Check the existing root password in .env.' }
    Write-Host 'Database 16-WebCalculatorDb is ready; existing databases and passwords are preserved.'
} finally { Pop-Location }
