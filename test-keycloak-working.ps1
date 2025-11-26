# Test Keycloak via Gateway
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "`n=== Testing Keycloak via Gateway ===" -ForegroundColor Cyan

# Wait a bit for Keycloak to be fully ready
Write-Host "Waiting 5 seconds for Keycloak to be fully ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test 1: Keycloak root via Gateway
Write-Host "`n[1] Testing Gateway proxy to Keycloak root..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest "https://localhost:7019/keycloak/" -UseBasicParsing
    Write-Host "SUCCESS - Keycloak accessible via Gateway (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    $status = $_.Exception.Response.StatusCode.value__
    Write-Host "Status: $status - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Keycloak health endpoint
Write-Host "`n[2] Testing Keycloak health..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod "https://localhost:7019/keycloak/health"
    Write-Host "SUCCESS - Keycloak healthy" -ForegroundColor Green
    Write-Host "Status: $($health.status)" -ForegroundColor Gray
} catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Try to access master realm (default realm)
Write-Host "`n[3] Testing master realm discovery..." -ForegroundColor Yellow
try {
    $discovery = Invoke-RestMethod "https://localhost:7019/keycloak/realms/master/.well-known/openid-configuration"
    Write-Host "SUCCESS - Master realm accessible" -ForegroundColor Green
    Write-Host "Issuer: $($discovery.issuer)" -ForegroundColor Gray
} catch {
    Write-Host "Master realm: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Try artauction realm (custom realm - may not exist yet)
Write-Host "`n[4] Testing artauction realm (custom)..." -ForegroundColor Yellow
try {
    $artauction = Invoke-RestMethod "https://localhost:7019/keycloak/realms/artauction/.well-known/openid-configuration"
    Write-Host "SUCCESS - ArtAuction realm exists!" -ForegroundColor Green
    Write-Host "Issuer: $($artauction.issuer)" -ForegroundColor Gray
} catch {
    Write-Host "ArtAuction realm not found - needs to be created/imported" -ForegroundColor Yellow
    Write-Host "Action: Import realm-artauction.json via Admin Console" -ForegroundColor Cyan
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Access Keycloak Admin Console:" -ForegroundColor White
Write-Host "   https://localhost:7019/keycloak/admin" -ForegroundColor Gray
Write-Host "   Login: admin / admin123" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Import custom realm:" -ForegroundColor White
Write-Host "   ArtAuction.AppHost\Keycloak\realm-artauction.json" -ForegroundColor Gray
Write-Host ""
