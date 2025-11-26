# Lab #9 Part 2: IdentityServer Integration - COMPLETED ✅

## Що реалізовано:

### 1. IdentityServer Infrastructure ✅
- **Duende.IdentityServer 7.3.2** project створено
- **3 SQL Server databases** через Aspire:
  - ApplicationDb (Users, Roles)
  - ConfigurationDb (Clients, Scopes, Resources)
  - PersistedGrantDb (Tokens, Keys)
- **EF Core Migrations** створено та застосовуються автоматично
- **Auto-seeding** при старті:
  - 4 Clients: postman, swagger, aggregator_service, web_client
  - 6 API Scopes: auctions.read/write, orders.read/write, admin, artauction.fullaccess
  - 3 API Resources: auctions_api, orders_api, artauction_api
  - 4 Identity Resources: openid, profile, email, roles
  - 3 Roles: Admin, User, AuctionManager
  - 2 Test users: admin@artauction.com / Admin@123, test@artauction.com / Test@123

### 2. WebApi Integration ✅
- **Dual Authentication**: IdentityServer (primary) + Legacy JWT (backwards compatible)
- **OAuth 2.0 Swagger UI** з Authorization Code + PKCE
- **Token validation** через IdentityServer Authority
- **Audience validation**: artauction_api, auctions_api

### 3. API Gateway Integration ✅
- **YARP route**: `/identity/**` → IdentityServer
- Проксування всіх OAuth endpoints через Gateway
- Service Discovery через Aspire

### 4. Aspire Orchestration ✅
- Service references налаштовано
- IdentityServer timeout збільшено до 180s
- Background database initialization

---

## Як тестувати OAuth 2.0:

### Крок 1: Запустити Aspire
```powershell
cd "c:\Work\Study\.NET Microservice\ProjectAuction"
dotnet run --project ArtAuction.AppHost
```

### Крок 2: Відкрити Swagger UI
**WebApi Swagger**: Перевірте URL в Aspire Dashboard → Resources → artauction-webapi → Endpoints

Наприклад: `https://localhost:7XXX/swagger`

### Крок 3: Автентифікація через OAuth 2.0

#### У Swagger UI:
1. Клікніть кнопку **Authorize** (замок)
2. У секції **oauth2** поставте галочки на scopes:
   - ✅ openid
   - ✅ profile
   - ✅ auctions.read
   - ✅ auctions.write
   - ✅ artauction.fullaccess
3. Клікніть **Authorize**
4. Відкриється вікно логіну IdentityServer
5. Введіть credentials:
   - **Email**: admin@artauction.com
   - **Password**: Admin@123
6. Клікніть **Yes, Allow** (consent screen)
7. Токен автоматично додасться до всіх запитів

#### Тестування API:
- Спробуйте виконати GET `/api/auctions` - має працювати з IdentityServer токеном
- Перевірте, що без токена повертається 401 Unauthorized

### Крок 4: Тестування через Gateway

**Gateway URL**: Знайдіть в Aspire Dashboard → api-gateway → Endpoints

OAuth endpoints через Gateway:
```
https://gateway-url/identity/.well-known/openid-configuration
https://gateway-url/identity/connect/authorize
https://gateway-url/identity/connect/token
```

### Крок 5: Тестування Client Credentials (M2M)

**PowerShell**:
```powershell
# Знайдіть IdentityServer URL в Dashboard
$idsUrl = "https://localhost:7XXX"  # Замініть на actual URL

$body = @{
    grant_type = "client_credentials"
    client_id = "aggregator_service"
    client_secret = "aggregator_secret_key_change_in_production"
    scope = "auctions.read orders.read artauction.fullaccess"
}

$token = Invoke-RestMethod -Uri "$idsUrl/connect/token" -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"

Write-Host "Access Token: $($token.access_token)"
Write-Host "Expires in: $($token.expires_in) seconds"

# Використати токен для виклику API
$headers = @{
    Authorization = "Bearer $($token.access_token)"
}
Invoke-RestMethod -Uri "https://webapi-url/api/auctions" -Headers $headers
```

---

## OAuth 2.0 Clients Configuration

| Client ID | Grant Type | Secret | PKCE | Redirect URI | Scopes |
|-----------|------------|--------|------|--------------|--------|
| `postman` | Authorization Code | ❌ No | ✅ Yes | https://oauth.pstmn.io/v1/callback | openid, profile, email, roles, auctions.*, orders.* |
| `swagger` | Authorization Code | ❌ No | ✅ Yes | https://localhost:7XXX/swagger/oauth2-redirect.html | openid, profile, auctions.* |
| `aggregator_service` | Client Credentials | ✅ Yes | ❌ No | N/A | auctions.read, orders.read, artauction.fullaccess |
| `web_client` | Authorization Code | ❌ No | ✅ Yes | https://localhost:3000/callback | openid, profile, email, roles, auctions.*, orders.*, admin |

---

## API Endpoints через Gateway

### IdentityServer (OAuth 2.0)
```
GET  /identity/.well-known/openid-configuration  # Discovery
GET  /identity/connect/authorize                 # Authorization endpoint
POST /identity/connect/token                     # Token endpoint
GET  /identity/connect/userinfo                  # User info
POST /identity/connect/introspect                # Token introspection
POST /identity/connect/revocation                # Token revocation
```

### Auth Service (Legacy JWT)
```
POST /auth/register                              # User registration
POST /auth/login                                 # JWT login
POST /auth/refresh-token                         # Refresh token
POST /auth/logout                                # Logout
GET  /users                                      # List users (Admin)
```

### WebApi (Protected by IdentityServer)
```
GET  /api/auctions                               # List auctions (auctions.read)
POST /api/auctions                               # Create auction (auctions.write)
GET  /api/auctions/{id}                          # Get auction (auctions.read)
PUT  /api/auctions/{id}                          # Update auction (auctions.write)
```

---

## Token Claims Structure

### IdentityServer Access Token (JWT):
```json
{
  "nbf": 1700000000,
  "exp": 1700003600,
  "iss": "https://localhost:7254",
  "aud": ["artauction_api", "auctions_api"],
  "client_id": "swagger",
  "sub": "user_guid",
  "auth_time": 1700000000,
  "idp": "local",
  "email": "admin@artauction.com",
  "role": "Admin",
  "scope": ["openid", "profile", "auctions.read", "auctions.write"],
  "amr": ["pwd"]
}
```

---

## Troubleshooting

### IdentityServer не запускається
- Перевірте Console logs в Aspire Dashboard
- Перевірте, що SQL Server контейнер запущений
- Збільште timeout: вже налаштовано 180s

### Token validation fails
- Переконайтесь, що WebApi може досягнути IdentityServer URL
- Перевірте Aspire Service Discovery
- Додайте логування в WebApi для діагностики

### Swagger OAuth не працює
- Перевірте, що client_id="swagger" існує в IdentityServer Config.cs
- Перевірте redirect URI у Swagger config
- Відкрийте DevTools → Network для перегляду OAuth redirect flow

### CORS errors
- IdentityServer автоматично дозволяє CORS для налаштованих redirect URIs
- Для додавання нових origins оновіть Config.cs → Client → AllowedCorsOrigins

---

## Наступні кроки (Lab #9 Part 3: Keycloak)

1. Додати Keycloak container через Aspire
2. Створити Pulumi project для IaC provisioning
3. Налаштувати Realm, Clients, Roles через Pulumi
4. Реалізувати Permission-Based Authorization
5. Створити custom [RequirePermission] attribute
6. Інтегрувати Gateway з Keycloak

---

**Статус**: ✅ IdentityServer Integration COMPLETE
**Build**: ✅ Successful (3 warnings, 0 errors)
**Ready for**: Testing OAuth 2.0 flows
