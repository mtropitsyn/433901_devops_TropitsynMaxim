param([ValidateRange(1,1000)][int]$Limit = 20)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $sql = @"
SELECT ID_DataInputVariant,
       TRIM(TRAILING '.' FROM TRIM(TRAILING '0' FROM CAST(Operand_1 AS CHAR))) AS Operand_1,
       TRIM(TRAILING '.' FROM TRIM(TRAILING '0' FROM CAST(Operand_2 AS CHAR))) AS Operand_2,
       Type_operation,
       TRIM(TRAILING '.' FROM TRIM(TRAILING '0' FROM CAST(Result AS CHAR))) AS Result
FROM DataInputVariants
ORDER BY ID_DataInputVariant DESC LIMIT $Limit;
"@
    & docker compose exec -T db sh -c 'MYSQL_PWD="$MARIADB_PASSWORD" exec mariadb --user="$MARIADB_USER" --database="$MARIADB_DATABASE" --table --execute="$1"' sh $sql
    if ($LASTEXITCODE -ne 0) { throw 'Could not read calculations from MariaDB.' }
} finally { Pop-Location }
