# Keycloak Diagnostic Script
Write-Host "`n=== Keycloak Diagnostics ===" -ForegroundColor Cyan

# Test 1: Check if Keycloak is accessible directly
Write-Host "`n[1] Testing direct Keycloak access on common ports..." -ForegroundColor Yellow

$keycloakPorts = @(8080, 8180, 9000)
$keycloakUrl = $null

foreach ($port in $keycloakPorts) {
    try {
        $testUrl = "http://localhost:$port"
        Write-Host "  Trying $testUrl..." -NoNewline
        $response = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 2 -UseBasicParsing 2>$null
        Write-Host " OK" -ForegroundColor Green
        $keycloakUrl = $testUrl
        break
    } catch {
        Write-Host " Not found" -ForegroundColor Gray
    }
}

if ($keycloakUrl) {
    Write-Host "`nKeycloak found at: $keycloakUrl" -ForegroundColor Green
    
    # Test 2: Check Keycloak health
    Write-Host "`n[2] Testing Keycloak health endpoint..." -ForegroundColor Yellow
    try {
        $health = Invoke-WebRequest -Uri "$keycloakUrl/health" -UseBasicParsing
        Write-Host "  Health Status: $($health.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "  Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Test 3: Check if realm exists
    Write-Host "`n[3] Testing artauction realm..." -ForegroundColor Yellow
    try {
        $discovery = Invoke-RestMethod "$keycloakUrl/realms/artauction/.well-known/openid-configuration"
        Write-Host "  Realm found!" -ForegroundColor Green
        Write-Host "  Issuer: $($discovery.issuer)" -ForegroundColor Gray
    } catch {
        Write-Host "  Realm not found - needs to be created/imported" -ForegroundColor Yellow
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
} else {
    Write-Host "`nKeycloak NOT FOUND on any standard port" -ForegroundColor Red
    Write-Host "Check Aspire Dashboard for actual Keycloak URL" -ForegroundColor Yellow
}

# Test 4: Check Gateway proxy
Write-Host "`n[4] Testing Gateway Keycloak proxy..." -ForegroundColor Yellow
try {
    $gatewayProxy = Invoke-WebRequest -Uri "https://localhost:7019/keycloak/" -UseBasicParsing
    Write-Host "  Gateway proxy: OK ($($gatewayProxy.StatusCode))" -ForegroundColor Green
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "  Gateway proxy failed: $statusCode" -ForegroundColor Red
    if ($statusCode -eq 502) {
        Write-Host "  -> Gateway cannot reach Keycloak (Bad Gateway)" -ForegroundColor Yellow
        Write-Host "  -> Possible causes:" -ForegroundColor Yellow
        Write-Host "     1. Keycloak container not started" -ForegroundColor Gray
        Write-Host "     2. Service Discovery not working" -ForegroundColor Gray
        Write-Host "     3. Keycloak still initializing" -ForegroundColor Gray
    }
}

Write-Host "`n=== Recommendations ===" -ForegroundColor Cyan
Write-Host "1. Check Aspire Dashboard -> Resources -> keycloak" -ForegroundColor White
Write-Host "2. Verify keycloak and postgres containers are Running" -ForegroundColor White
Write-Host "3. Check keycloak logs for errors" -ForegroundColor White
Write-Host "4. Keycloak may need 1-2 minutes to fully start" -ForegroundColor White
Write-Host ""
