param([ValidateRange(1,1000)][int]$Limit = 20)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $sql = @"
SELECT id,
       TRIM(TRAILING '.' FROM TRIM(TRAILING '0' FROM CAST(first_operand AS CHAR))) AS first_operand,
       TRIM(TRAILING '.' FROM TRIM(TRAILING '0' FROM CAST(second_operand AS CHAR))) AS second_operand,
       operation,
       TRIM(TRAILING '.' FROM TRIM(TRAILING '0' FROM CAST(result AS CHAR))) AS result,
       created_at
FROM calculations
ORDER BY id DESC LIMIT $Limit;
"@
    & docker compose exec -T db sh -c 'MYSQL_PWD="$MARIADB_PASSWORD" exec mariadb --user="$MARIADB_USER" --database="$MARIADB_DATABASE" --table --execute="$1"' sh $sql
    if ($LASTEXITCODE -ne 0) { throw 'Could not read calculations from MariaDB.' }
} finally { Pop-Location }
