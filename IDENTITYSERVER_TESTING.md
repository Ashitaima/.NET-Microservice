# IdentityServer Testing Guide

## Статус: IdentityServer Infrastructure Complete ✅

### Що реалізовано:

1. **Duende IdentityServer 7.3.2** project створено
2. **3 SQL Server databases** налаштовано через Aspire:
   - `ApplicationDb` - ASP.NET Identity (Users, Roles)
   - `ConfigurationDb` - Clients, API Resources, Scopes
   - `PersistedGrantDb` - Tokens, Authorization Codes, Consents
3. **EF Core Migrations** створено для всіх 3 БД
4. **Auto-migration + Seeding** на старті:
   - Clients: postman, swagger, aggregator_service, web_client
   - API Scopes: auctions.read/write, orders.read/write, admin, artauction.fullaccess
   - API Resources: auctions_api, orders_api, artauction_api
   - Identity Resources: openid, profile, email, roles
   - Default roles: Admin, User, AuctionManager
   - Test users:
     - `admin@artauction.com` / `Admin@123` (Admin role)
     - `test@artauction.com` / `Test@123` (User role)

---

## Крок 1: Знайти URL IdentityServer в Aspire Dashboard

1. Відкрийте: **https://localhost:17181**
2. Перейдіть на вкладку **Resources**
3. Знайдіть сервіс **identityserver**
4. Перевірте:
   - **Status**: має бути `Running` (зелений)
   - **Endpoints**: запишіть URL (наприклад, `https://localhost:7XXX`)

**Якщо Status = "Failed to start"**:
- Клікніть на `identityserver` → вкладка **Console**
- Перевірте логи на помилки (connection strings, migrations, тощо)
- Клікніть на **Traces** для детальної діагностики

---

## Крок 2: Тестування Discovery Endpoint

Використайте знайдений URL (замініть `{URL}`):

```powershell
# PowerShell 5.1 compatible
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# Trust self-signed cert
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint svcPoint, X509Certificate certificate,
            WebRequest webRequest, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

# Test Discovery
$url = "{URL}/.well-known/openid-configuration"
$discovery = Invoke-RestMethod -Uri $url -Method Get
$discovery | ConvertTo-Json -Depth 3
```

**Очікуваний результат**:
```json
{
  "issuer": "{URL}",
  "authorization_endpoint": "{URL}/connect/authorize",
  "token_endpoint": "{URL}/connect/token",
  "userinfo_endpoint": "{URL}/connect/userinfo",
  "introspection_endpoint": "{URL}/connect/introspect",
  "scopes_supported": [
    "openid", "profile", "email", "roles",
    "auctions.read", "auctions.write",
    "orders.read", "orders.write",
    "admin", "artauction.fullaccess"
  ],
  "grant_types_supported": [
    "authorization_code", "client_credentials", "refresh_token"
  ]
}
```

---

## Крок 3: Тестування Client Credentials Flow

### 3.1 Отримання токена (M2M)

```powershell
$tokenUrl = "{URL}/connect/token"
$body = @{
    grant_type = "client_credentials"
    client_id = "aggregator_service"
    client_secret = "aggregator_secret_key_change_in_production"
    scope = "auctions.read orders.read artauction.fullaccess"
}

$token = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
Write-Host "Access Token: $($token.access_token)"
Write-Host "Expires in: $($token.expires_in) seconds"
```

### 3.2 Introspection (перевірка токена)

```powershell
$introspectionUrl = "{URL}/connect/introspect"
$introspectionBody = @{
    token = $token.access_token
    client_id = "aggregator_service"
    client_secret = "aggregator_secret_key_change_in_production"
}

$introspection = Invoke-RestMethod -Uri $introspectionUrl -Method Post -Body $introspectionBody -ContentType "application/x-www-form-urlencoded"
$introspection | ConvertTo-Json
```

**Очікуваний результат**:
```json
{
  "active": true,
  "client_id": "aggregator_service",
  "scope": "auctions.read orders.read artauction.fullaccess",
  "aud": ["auctions_api", "orders_api", "artauction_api"]
}
```

---

## Крок 4: Тестування Authorization Code Flow (Postman)

### 4.1 Налаштування Postman

1. **Authorization Type**: OAuth 2.0
2. **Grant Type**: Authorization Code (with PKCE)
3. **Auth URL**: `{URL}/connect/authorize`
4. **Access Token URL**: `{URL}/connect/token`
5. **Client ID**: `postman`
6. **Client Secret**: (залиште порожнім - public client)
7. **Scope**: `openid profile email roles auctions.read auctions.write`
8. **Redirect URI**: `https://oauth.pstmn.io/v1/callback`

### 4.2 Отримання токена

1. Клікніть **Get New Access Token**
2. Відкриється вікно логіну IdentityServer
3. Залогіньтесь як:
   - Email: `admin@artauction.com`
   - Password: `Admin@123`
4. Надайте consent (якщо попросить)
5. Отримаєте access_token + refresh_token

### 4.3 Декодування токена

Скопіюйте `access_token` та вставте на **https://jwt.io**

**Очікувані claims**:
```json
{
  "sub": "user_id_guid",
  "email": "admin@artauction.com",
  "role": "Admin",
  "scope": ["openid", "profile", "email", "roles", "auctions.read", "auctions.write"],
  "client_id": "postman",
  "aud": "artauction_api"
}
```

---

## Крок 5: Перевірка Databases через SQL Server Management Studio

### Connection Strings (знайдіть у Aspire Dashboard → Resources → SQL Server)

Приклад:
```
Server=localhost,<PORT>;User Id=sa;Password=<PASSWORD>;TrustServerCertificate=True
```

### Перевірте таблиці:

**ApplicationDb**:
- `Users` - має містити 2 користувачів (admin, test)
- `Roles` - має містити 3 ролі (Admin, User, AuctionManager)
- `UserRoles` - має містити зв'язки

**ConfigurationDb**:
- `Clients` - 4 клієнти
- `ApiScopes` - 6 scopes
- `ApiResources` - 3 API ресурси
- `IdentityResources` - 4 identity ресурси

**PersistedGrantDb**:
- `Keys` - автоматично згенеровані ключі для підпису токенів
- `PersistedGrants` - токени (з'являються після використання)

---

## Наступні кроки

### 1. Інтегрувати WebApi з IdentityServer

**ArtAuction.WebApi/appsettings.json**:
```json
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "Authority": "{IDENTITYSERVER_URL}",
        "ValidAudience": "artauction_api",
        "RequireHttpsMetadata": false
      }
    }
  }
}
```

### 2. Інтегрувати AuctionService з IdentityServer

**AuctionService.Api/appsettings.json**:
```json
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "Authority": "{IDENTITYSERVER_URL}",
        "ValidAudience": "auctions_api",
        "RequireHttpsMetadata": false
      }
    }
  }
}
```

### 3. Налаштувати API Gateway (YARP)

**ArtAuction.ApiGateway/appsettings.json** - додати маршрут:
```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": {
          "Path": "/identity/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/identity" }
        ]
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "{IDENTITYSERVER_URL}"
          }
        }
      }
    }
  }
}
```

### 4. Налаштувати Swagger OAuth

**Program.cs** у WebApi та AuctionService:
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("{IDENTITYSERVER_URL}/connect/authorize"),
                TokenUrl = new Uri("{IDENTITYSERVER_URL}/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "auctions.read", "Read access to auctions" },
                    { "auctions.write", "Write access to auctions" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { "auctions.read", "auctions.write" }
        }
    });
});

// After app.UseSwagger()
app.UseSwaggerUI(c =>
{
    c.OAuthClientId("swagger");
    c.OAuthUsePkce();
});
```

---

## Troubleshooting

### IdentityServer не запускається

1. Перевірте Console logs у Aspire Dashboard
2. Типові помилки:
   - **Connection string invalid**: Перевірте, чи SQL Server контейнер запущений
   - **Migration failed**: Видаліть папку `Data/Migrations` та створіть нові
   - **DeveloperSigningCredential error**: Переконайтесь, що `.AddDeveloperSigningCredential()` є в Program.cs

### Discovery endpoint повертає 404

- Перевірте URL (має бути HTTPS)
- Переконайтесь, що middleware `app.UseIdentityServer()` додано ДО `app.UseAuthorization()`

### Token validation fails у microservices

- Перевірте, що `Authority` вказує на ПРАВИЛЬНИЙ IdentityServer URL
- Перевірте, що `ValidAudience` відповідає `ApiResource.Name` з Config.cs
- Додайте `RequireHttpsMetadata = false` для development

---

## OAuth 2.0 Clients Configuration

| Client ID | Grant Type | Secret Required | Redirect URI | Scopes |
|-----------|------------|----------------|--------------|--------|
| `postman` | Authorization Code + PKCE | ❌ No | `https://oauth.pstmn.io/v1/callback` | openid, profile, email, roles, auctions.*, orders.* |
| `swagger` | Authorization Code + PKCE | ❌ No | `https://localhost:7001/swagger/oauth2-redirect.html` | openid, profile, auctions.* |
| `aggregator_service` | Client Credentials | ✅ Yes | N/A | auctions.read, orders.read, artauction.fullaccess |
| `web_client` | Authorization Code + PKCE | ❌ No | `https://localhost:3000/callback` | openid, profile, email, roles, auctions.*, orders.*, admin |

---

**Успіхів з тестуванням! 🚀**
