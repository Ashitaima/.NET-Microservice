# OAuth 2.0 Testing Guide - Manual Steps

## Поточний статус:
- ✅ IdentityServer project створено і налаштовано
- ✅ WebApi integration з OAuth 2.0 Swagger
- ✅ Gateway YARP proxy для /identity/**
- ✅ Aspire запущено на https://localhost:17181
- ⏳ Тестування OAuth flows

## Кроки для тестування:

### 1. Знайти URLs сервісів в Aspire Dashboard

**Відкрийте Dashboard**: https://localhost:17181

1. Перейдіть на вкладку **Resources**
2. Знайдіть сервіси та запишіть їх URLs:

| Service | Name in Dashboard | Port Pattern | Endpoint to Check |
|---------|------------------|--------------|-------------------|
| IdentityServer | identityserver | 7XXX | `/.well-known/openid-configuration` |
| WebApi | artauction-webapi | 7YYY | `/swagger` |
| Gateway | api-gateway | 7ZZZ | `/identity/.well-known/openid-configuration` |

**Приклад**: Якщо ви бачите `https://localhost:7254` для identityserver, то:
- Discovery endpoint: `https://localhost:7254/.well-known/openid-configuration`
- Token endpoint: `https://localhost:7254/connect/token`

---

### 2. Тест Discovery Endpoint (PowerShell)

```powershell
# Замініть 7254 на actual port з Dashboard
$idsUrl = "https://localhost:7254"

# SSL workaround
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
add-type @"
using System.Net;using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy:ICertificatePolicy{public bool CheckValidationResult(ServicePoint s,X509Certificate c,WebRequest w,int p){return true;}}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

# Get Discovery Document
$discovery = Invoke-RestMethod -Uri "$idsUrl/.well-known/openid-configuration"
$discovery | ConvertTo-Json
```

**Очікуваний результат**:
```json
{
  "issuer": "https://localhost:7254",
  "authorization_endpoint": "https://localhost:7254/connect/authorize",
  "token_endpoint": "https://localhost:7254/connect/token",
  "scopes_supported": ["openid", "profile", "email", "roles", "auctions.read", ...]
}
```

---

### 3. Тест Client Credentials Flow (M2M Authentication)

```powershell
$tokenUrl = "$idsUrl/connect/token"

$body = @{
    grant_type = "client_credentials"
    client_id = "aggregator_service"
    client_secret = "aggregator_secret_key_change_in_production"
    scope = "auctions.read orders.read artauction.fullaccess"
}

$token = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"

Write-Host "Access Token: $($token.access_token)"
Write-Host "Token Type: $($token.token_type)"
Write-Host "Expires In: $($token.expires_in) seconds"
Write-Host "Scope: $($token.scope)"
```

**Успіх**: Ви отримаєте JWT токен

**Збережіть токен**:
```powershell
$accessToken = $token.access_token
```

---

### 4. Тест Token Introspection

```powershell
$introspectionUrl = "$idsUrl/connect/introspect"

$introBody = @{
    token = $accessToken
    client_id = "aggregator_service"
    client_secret = "aggregator_secret_key_change_in_production"
}

$introspection = Invoke-RestMethod -Uri $introspectionUrl -Method Post -Body $introBody -ContentType "application/x-www-form-urlencoded"

$introspection | ConvertTo-Json
```

**Очікуваний результат**:
```json
{
  "active": true,
  "client_id": "aggregator_service",
  "token_type": "Bearer",
  "scope": "auctions.read orders.read artauction.fullaccess"
}
```

---

### 5. Тест API з IdentityServer Token

```powershell
# Замініть на actual WebApi URL з Dashboard
$apiUrl = "https://localhost:7XXX"

$headers = @{
    Authorization = "Bearer $accessToken"
}

# Test protected endpoint
Invoke-RestMethod -Uri "$apiUrl/api/auctions" -Headers $headers
```

**Успіх**: API повертає дані (або 200 OK з порожнім масивом)
**Помилка 401**: Token validation не працює - потрібно перевірити конфігурацію

---

### 6. Тест Gateway Proxy

```powershell
# Замініть на actual Gateway URL з Dashboard
$gatewayUrl = "https://localhost:7ZZZ"

# Discovery через Gateway
$gwDiscovery = Invoke-RestMethod -Uri "$gatewayUrl/identity/.well-known/openid-configuration"

Write-Host "Issuer via Gateway: $($gwDiscovery.issuer)"
```

**Успіх**: Discovery повертається через Gateway proxy

---

### 7. Тест Swagger OAuth UI (Interactive)

1. **Відкрийте Swagger UI**: `https://localhost:7YYY/swagger` (WebApi URL з Dashboard)

2. **Клікніть кнопку "Authorize"** (праворуч зверху, іконка замка)

3. **У секції oauth2**:
   - ✅ Поставте галочки на scopes:
     - openid
     - profile
     - auctions.read
     - auctions.write
     - artauction.fullaccess
   - Клікніть **Authorize**

4. **Відкриється вікно IdentityServer Login**:
   - Email: `admin@artauction.com`
   - Password: `Admin@123`
   - Клікніть **Login**

5. **Consent Screen** (якщо з'явиться):
   - Клікніть **Yes, Allow**

6. **Токен автоматично додано** до всіх запитів

7. **Тестуйте endpoints**:
   - Спробуйте `GET /api/auctions`
   - Має працювати з 200 OK
   - Без токена - 401 Unauthorized

---

### 8. Декодування JWT Token (опціонально)

**Онлайн**: Вставте токен на https://jwt.io

**PowerShell**:
```powershell
$parts = $accessToken.Split('.')
$payload = $parts[1]
while ($payload.Length % 4 -ne 0) { $payload += "=" }
$json = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
$claims = $json | ConvertFrom-Json
$claims | ConvertTo-Json
```

**Очікувані claims**:
```json
{
  "client_id": "aggregator_service",
  "aud": ["artauction_api", "auctions_api"],
  "scope": ["auctions.read", "orders.read", "artauction.fullaccess"],
  "exp": 1700003600,
  "iss": "https://localhost:7254"
}
```

---

## Troubleshooting

### Сервіси не запускаються
- Перевірте Console logs в Dashboard → Resources → (service) → Console
- Перевірте Traces для детальної діагностики

### Discovery endpoint 404
- Переконайтесь, що IdentityServer запущений (Status = Running)
- Використовуйте правильний URL з Dashboard

### Token validation fails (401)
- Перевірте, що WebApi може досягнути IdentityServer
- Перевірте логи WebApi в Dashboard
- Audience має бути "artauction_api" або "auctions_api"

### Swagger OAuth не відкривається
- Перевірте, що client "swagger" існує в Config.cs
- Перевірте redirect URI: має відповідати `{webapi_url}/swagger/oauth2-redirect.html`

---

## Успішний результат

✅ Discovery endpoint доступний
✅ Client Credentials flow працює
✅ Token introspection повертає active=true
✅ API приймає IdentityServer токени
✅ Gateway проксує /identity/** endpoints
✅ Swagger OAuth login працює

---

**Готово до production**? Ні! Потрібно:
- Замінити DeveloperSigningCredential на proper certificate
- Змінити client secrets
- Увімкнути HTTPS (RequireHttpsMetadata = true)
- Налаштувати CORS policies
- Додати rate limiting
- Налаштувати logging & monitoring
