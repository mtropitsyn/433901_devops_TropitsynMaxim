param([string]$BaseUrl = 'http://localhost:5016', [switch]$VerifyDatabaseRestart)
$ErrorActionPreference = 'Stop'
$checks = 0
function Assert-DatabaseCheck([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $script:checks++
    Write-Host "PASS: $message"
}
function Invoke-DatabaseSql([string]$Sql) {
    $rows = & docker compose exec -T db sh -c 'MYSQL_PWD="$MARIADB_PASSWORD" exec mariadb --user="$MARIADB_USER" --database="$MARIADB_DATABASE" --batch --skip-column-names --execute="$1"' sh $Sql
    if ($LASTEXITCODE -ne 0) { throw 'MariaDB query failed.' }
    $rows
}
function Submit-Calculation([string]$first, [string]$second, [string]$operation) {
    $page = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -SessionVariable requestSession
    $token = [regex]::Match($page.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $token.Success) { throw 'Antiforgery token was not found.' }
    $body = @{ FirstNumber=$first; SecondNumber=$second; Operation=$operation; __RequestVerificationToken=$token.Groups[1].Value }
    $response = Invoke-WebRequest -Uri $BaseUrl -Method Post -Body $body -WebSession $requestSession -UseBasicParsing -TimeoutSec 20
    [System.Net.WebUtility]::HtmlDecode($response.Content)
}
function As-Decimal([string]$value) {
    [decimal]::Parse($value, [Globalization.CultureInfo]::InvariantCulture)
}
function Wait-Database {
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        try {
            $health = Invoke-WebRequest -Uri "$BaseUrl/health/ready" -UseBasicParsing -TimeoutSec 8
            if ($health.Content -eq 'OK') { return }
        } catch { }
        Start-Sleep -Seconds 1
    }
    throw 'Application did not reconnect to MariaDB.'
}
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    Wait-Database
    $baselineId = [long](Invoke-DatabaseSql 'SELECT COALESCE(MAX(id), 0) FROM calculations;')
    $baselineCount = [long](Invoke-DatabaseSql 'SELECT COUNT(*) FROM calculations;')
    & (Join-Path $PSScriptRoot 'Test-Web.ps1') -BaseUrl $BaseUrl
    $rows = @(Invoke-DatabaseSql "SELECT first_operand, second_operand, operation, result FROM calculations WHERE id > $baselineId ORDER BY id;")
    Assert-DatabaseCheck ($rows.Count -eq 5) 'five successful HTTP calculations create exactly five records; invalid requests create none'
    $expected = @(@('12','4','add','16'), @('12','4','subtract','8'), @('-3','4','multiply','-12'), @('7','2','divide','3.5'), @('0.1','0.2','add','0.3'))
    for ($i=0; $i -lt $expected.Count; $i++) {
        $fields = $rows[$i] -split "`t"
        $case = $expected[$i]
        Assert-DatabaseCheck ($fields.Count -eq 4 -and (As-Decimal $fields[0]) -eq (As-Decimal $case[0]) -and (As-Decimal $fields[1]) -eq (As-Decimal $case[1]) -and $fields[2] -eq $case[2] -and (As-Decimal $fields[3]) -eq (As-Decimal $case[3])) "MariaDB stores operands, operation and result for $($case[2])"
    }
    foreach ($case in @(
        @('79228162514264337593543950335','0','add','79228162514264337593543950335'),
        @('0.0000000000000000000000000001','1','multiply','0.0000000000000000000000000001'),
        @('1','3','divide','0.3333333333333333333333333333'),
        @('-12.5','-2','divide','6.25')
    )) {
        $html = Submit-Calculation $case[0] $case[1] $case[2]
        if (-not $html.Contains('id="calculation-result"')) { throw 'Precision calculation failed.' }
        $fields = (Invoke-DatabaseSql 'SELECT first_operand, second_operand, operation, result FROM calculations ORDER BY id DESC LIMIT 1;') -split "`t"
        Assert-DatabaseCheck ((As-Decimal $fields[0]) -eq (As-Decimal $case[0]) -and (As-Decimal $fields[1]) -eq (As-Decimal $case[1]) -and $fields[2] -eq $case[2] -and (As-Decimal $fields[3]) -eq (As-Decimal $case[3])) "MariaDB preserves decimal precision [$($case[0]) $($case[2]) $($case[1])]"
    }
    $count = [long](Invoke-DatabaseSql 'SELECT COUNT(*) FROM calculations;')
    Assert-DatabaseCheck ($count -eq $baselineCount + 9) 'all successful calculations are stored once'
    if ($VerifyDatabaseRestart) {
        & docker compose stop db
        if ($LASTEXITCODE -ne 0) { throw 'Could not stop MariaDB.' }
        try {
            $html = Submit-Calculation '101' '202' 'add'
            Assert-DatabaseCheck ($html.Contains('Не удалось сохранить расчёт в базе данных') -and -not $html.Contains('id="calculation-result"')) 'database outage produces a visible error instead of an unsaved result'
        } finally {
            # Recreate the database container using the same volume to verify persistence.
            & docker compose up -d --wait --force-recreate db
            if ($LASTEXITCODE -ne 0) { throw 'Could not restart MariaDB.' }
        }
        Wait-Database
        Assert-DatabaseCheck ([long](Invoke-DatabaseSql 'SELECT COUNT(*) FROM calculations;') -eq $count) 'history survives MariaDB container recreation; failed save creates no record'
        $html = Submit-Calculation '101' '202' 'add'
        $stored = [long](Invoke-DatabaseSql 'SELECT COUNT(*) FROM calculations;')
        $correct = [long](Invoke-DatabaseSql 'SELECT COUNT(*) FROM calculations WHERE id = (SELECT MAX(id) FROM calculations) AND first_operand=101 AND second_operand=202 AND operation="add" AND result=303;')
        Assert-DatabaseCheck ($html.Contains('id="calculation-result">303</output>') -and $stored -eq $count + 1 -and $correct -eq 1) 'application reconnects and saves calculations after MariaDB restart'
    }
    Write-Host "Database checks passed: $checks"
} finally { Pop-Location }
