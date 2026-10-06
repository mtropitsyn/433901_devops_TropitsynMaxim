param([string]$BaseUrl = 'http://localhost:5016')
$ErrorActionPreference = 'Stop'
$checks = 0
function Assert-Check([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    $script:checks++
    Write-Host "PASS: $message"
}
function Submit-Calculation([string]$first, [string]$second, [string]$operation) {
    $page = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -SessionVariable requestSession
    $match = [regex]::Match($page.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'Antiforgery token was not found.' }
    $body = @{ FirstNumber=$first; SecondNumber=$second; Operation=$operation; __RequestVerificationToken=$match.Groups[1].Value }
    $response = Invoke-WebRequest -Uri $BaseUrl -Method Post -Body $body -WebSession $requestSession -UseBasicParsing
    [System.Net.WebUtility]::HtmlDecode($response.Content)
}
$health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing
Assert-Check ($health.StatusCode -eq 200 -and $health.Content -eq 'OK') 'health endpoint'
$style = Invoke-WebRequest -Uri "$BaseUrl/css/site.css" -UseBasicParsing
Assert-Check ($style.StatusCode -eq 200 -and $style.Content.Contains('.calculator-card')) 'stylesheet served'
foreach ($case in @(@('12','4','add','16'), @('12','4','subtract','8'), @('-3','4','multiply','-12'), @('7','2','divide','3,5'), @('0,1','0.2','add','0,3'))) {
    $html = Submit-Calculation $case[0] $case[1] $case[2]
    Assert-Check ($html -match ('id="calculation-result">' + [regex]::Escape($case[3]) + '</output>')) "HTTP $($case[2]) => $($case[3])"
}
foreach ($case in @(@('10','0','divide'), @('abc','2','add'), @('','','add'), @('1','2','invalid'), @('79228162514264337593543950335','2','multiply'))) {
    $html = Submit-Calculation $case[0] $case[1] $case[2]
    Assert-Check ($html.Contains('validation-summary-errors') -and -not $html.Contains('id="calculation-result"')) "HTTP rejects [$($case -join ', ')]"
}
try {
    Invoke-WebRequest -Uri $BaseUrl -Method Post -Body @{FirstNumber='1';SecondNumber='2';Operation='add'} -UseBasicParsing | Out-Null
    throw 'POST without antiforgery token was accepted.'
} catch {
    if (-not $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 400) { throw }
    Assert-Check $true 'POST without antiforgery token returns 400'
}
Write-Host "HTTP checks passed: $checks"

