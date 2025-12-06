using MediatR;
using ArtAuction.WebApi.Services;
using ArtAuction.WebApi.Extensions;
using ArtAuction.WebApi.Security;
using ArtAuction.Application;
using ArtAuction.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Global HTTP client handler factory to bypass SSL certificate validation in development
static HttpClientHandler CreateHttpClientHandler() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

// Aspire ServiceDefaults - OpenTelemetry, Serilog, Health Checks
builder.AddServiceDefaults();

// Configure global HttpClient factory to bypass SSL validation for all HTTP clients
builder.Services.AddHttpClient(Options.DefaultName)
    .ConfigurePrimaryHttpMessageHandler(() => CreateHttpClientHandler());

// Lab #4: Add Application and Infrastructure layers (Clean Architecture + CQRS + MongoDB)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication - Use IdentityServer as Authority
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() 
    ?? new JwtSettings();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// Get IdentityServer URL from Aspire Service Discovery
var identityServerUrl = builder.Configuration.GetConnectionString("identityserver") 
    ?? "https://localhost:7254"; // Fallback for local development

// Get Keycloak URL from Aspire Service Discovery (Lab #9 Part 3)
var keycloakUrl = builder.Configuration.GetConnectionString("keycloak") 
    ?? "http://localhost:8080"; // Fallback for local development

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, "IdentityServer or Keycloak or Legacy", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var authorization = context.Request.Headers.Authorization.ToString();
        
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization.Substring("Bearer ".Length).Trim();
            var jwtHandler = new JwtSecurityTokenHandler();

            // Перевіряємо, чи це валідний JWT формат
            if (jwtHandler.CanReadToken(token))
            {
                try
                {
                    // Читаємо токен без валідації підпису, щоб дізнатися хто його видав
                    var jwtToken = jwtHandler.ReadJwtToken(token);
                    
                    // Отримуємо Issuer з налаштувань або використовуємо дефолтний
                    var legacyIssuer = jwtSettings.Issuer ?? "ArtAuction.AuthService";

                    logger.LogInformation("Token Issuer: {Issuer}, Expected Legacy Issuer: {LegacyIssuer}", 
                        jwtToken.Issuer, legacyIssuer);

                    // Якщо токен виданий нашим старим сервісом -> використовуємо схему Legacy
                    if (jwtToken.Issuer.Equals(legacyIssuer, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("Selected scheme: Legacy");
                        return "Legacy";
                    }
                    
                    // Перевірка для Keycloak (якщо issuer містить realm)
                    if (jwtToken.Issuer?.Contains("/realms/", StringComparison.OrdinalIgnoreCase) == true ||
                        jwtToken.Issuer?.Contains("keycloak", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        logger.LogInformation("Selected scheme: Keycloak");
                        return "Keycloak";
                    }
                    
                    logger.LogInformation("Selected scheme: IdentityServer (default for issuer: {Issuer})", jwtToken.Issuer);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading JWT token");
                }
            }
            else
            {
                logger.LogWarning("Cannot read token as JWT");
            }
            
            // Для всіх інших токенів (IdentityServer)
            return "IdentityServer"; 
        }
        
        // Дефолтна схема, якщо немає заголовка Authorization
        logger.LogInformation("No Authorization header, using IdentityServer scheme");
        return "IdentityServer";
    };
})
.AddJwtBearer("IdentityServer", options =>
{
    options.RequireHttpsMetadata = false;
    options.MapInboundClaims = false;
    options.Authority = "https://localhost:7254";
    options.Audience = "artauction_api";
    
    // ASP.NET Core 8: Use legacy JwtSecurityTokenHandler for compatibility
    options.UseSecurityTokenValidators = true;
    
    // TEMPORARY: Disable signature validation for development
    // TODO: Fix JWKS retrieval with proper SSL certificate handling
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = false,
        ValidIssuer = "https://localhost:7254",
        ValidAudiences = new[] { "artauction_api", "auctions_api" },
        ClockSkew = TimeSpan.FromMinutes(5),
        RoleClaimType = "role",
        // Custom signature validator that bypasses signature verification
        SignatureValidator = (token, parameters) =>
        {
            // Read the token without validating the signature
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtHandler.ReadJwtToken(token);
            return jwtToken;
        }
    };
    
    // Configure HttpClient for metadata retrieval with SSL bypass
    options.BackchannelHttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    // Force metadata refresh when keys not found
    options.RefreshOnIssuerKeyNotFound = true;
    options.MetadataAddress = "https://localhost:7254/.well-known/openid-configuration";
    
    // Add event handlers for debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Authentication failed: {Error}", context.Exception.Message);
            
            // Detailed token info
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            {
                var token = authorization.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    logger.LogError("Token Details - Issuer: {Issuer}, Audiences: {Audiences}, Expires: {Exp}", 
                        jwtToken.Issuer,
                        string.Join(", ", jwtToken.Audiences),
                        jwtToken.ValidTo);
                    logger.LogError("Token Claims: {Claims}", 
                        string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}={c.Value}")));
                }
            }
            
            // Log configuration details
            if (context.Options?.ConfigurationManager != null)
            {
                try
                {
                    var config = context.Options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
                    logger.LogError("Configuration loaded. Signing keys count: {Count}", config.SigningKeys?.Count ?? 0);
                    logger.LogError("Issuer from config: {Issuer}", config.Issuer);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load OIDC configuration");
                }
            }
            
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✓✓✓ TOKEN VALIDATED SUCCESSFULLY ✓✓✓");
            logger.LogInformation("User: {User}, Role claims: {Roles}", 
                context.Principal?.Identity?.Name,
                string.Join(", ", context.Principal?.Claims.Where(c => c.Type == "role").Select(c => c.Value) ?? Array.Empty<string>()));
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            
            // Log all headers
            logger.LogInformation("=== REQUEST HEADERS ===");
            foreach (var header in context.Request.Headers)
            {
                logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
            }
            
            var authorization = context.Request.Headers.Authorization.ToString();
            logger.LogInformation("Authorization header value: '{Auth}'", authorization);
            
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            {
                logger.LogInformation("✓ Token found in Authorization header");
                var token = authorization.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    logger.LogInformation("Token claims: Issuer={Issuer}, Audiences={Audiences}, Expiration={Exp}", 
                        jwtToken.Issuer, 
                        string.Join(", ", jwtToken.Audiences),
                        jwtToken.ValidTo);
                    
                    foreach (var claim in jwtToken.Claims.Take(10))
                    {
                        logger.LogInformation("  Claim: {Type} = {Value}", claim.Type, claim.Value);
                    }
                }
            }
            
            return Task.CompletedTask;
        }
    };
})
.AddJwtBearer("Keycloak", options => // Lab #9 Part 3: Keycloak OAuth
{
    options.Authority = $"{keycloakUrl}/realms/artauction";
    options.RequireHttpsMetadata = false; // For dev only
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidAudiences = new[] { "artauction-api", "account" },
        ValidateIssuer = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.MetadataAddress = $"{keycloakUrl}/realms/artauction/.well-known/openid-configuration";
})
.AddJwtBearer("Legacy", options => // Keep old JWT for backwards compatibility
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});


// Lab #6: Add Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size to prevent memory leaks
    options.CompactionPercentage = 0.25; // Remove 25% when size limit reached
});

// Lab #6: Add Redis Distributed Cache via Aspire
builder.AddRedisClient("redis"); // Aspire Redis client
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
    options.InstanceName = "ArtAuction:";
});

// Lab #6: Add gRPC Services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
    options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
});

// Lab #7: Register custom metrics service
builder.Services.AddSingleton<AuctionMetricsService>();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Art Auction API - OAuth 2.0 with IdentityServer", Version = "v1" });
    
    // Add OAuth 2.0 Authentication to Swagger
    // IMPORTANT: Use public URL (localhost:7254) not Aspire internal URL
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://localhost:7254/connect/authorize"),
                TokenUrl = new Uri("https://localhost:7254/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" },
                    { "roles", "User roles" },
                    { "auctions.read", "Read access to auctions" },
                    { "auctions.write", "Write access to auctions" },
                    { "artauction.fullaccess", "Full access to Art Auction API" }
                }
            }
        }
    });

    // Keep Bearer token for backwards compatibility
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header (Legacy). Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { "openid", "profile", "roles", "auctions.read", "auctions.write", "artauction.fullaccess" }
        }
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(ArtAuction.Application.Common.Interfaces.ICommand).Assembly);

// CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Test IdentityServer connectivity at startup (non-blocking)
var logger = app.Services.GetRequiredService<ILogger<Program>>();
_ = Task.Run(async () =>
{
    await Task.Delay(2000); // Wait 2 seconds for IdentityServer to start
    try
    {
        using var httpClient = new HttpClient(CreateHttpClientHandler());
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var metadataUrl = "https://localhost:7254/.well-known/openid-configuration";
        logger.LogInformation("Testing connection to IdentityServer metadata at: {MetadataUrl}", metadataUrl);
        
        var response = await httpClient.GetAsync(metadataUrl);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("✓ Successfully retrieved IdentityServer metadata. Length: {Length} bytes", content.Length);
            
            // Parse and check JWKS URI
            var discovery = System.Text.Json.JsonDocument.Parse(content);
            if (discovery.RootElement.TryGetProperty("jwks_uri", out var jwksUri))
            {
                logger.LogInformation("✓ JWKS URI from discovery: {JwksUri}", jwksUri.GetString());
                
                // Try to fetch JWKS
                var jwksResponse = await httpClient.GetStringAsync(jwksUri.GetString());
                var jwks = System.Text.Json.JsonDocument.Parse(jwksResponse);
                var keysCount = jwks.RootElement.TryGetProperty("keys", out var keys) ? keys.GetArrayLength() : 0;
                logger.LogInformation("✓ JWKS endpoint accessible. Keys count: {Count}", keysCount);
            }
            else
            {
                logger.LogError("✗ jwks_uri not found in discovery document!");
            }
            
            logger.LogInformation("✓ IdentityServer is accessible and ready");
        }
        else
        {
            logger.LogWarning("✗ Failed to retrieve IdentityServer metadata. Status: {Status}", response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "✗ Cannot connect to IdentityServer at https://localhost:7254 - it may not be running or still starting up");
    }
});

// Lab #7: Health check endpoints - liveness and readiness
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    AllowCachingResponses = false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckExtensions.WriteHealthCheckResponse
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckExtensions.WriteHealthCheckResponse
});

// Aspire ServiceDefaults middleware (logging, tracing)
// Note: MapDefaultEndpoints() also adds /health and /alive endpoints
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Art Auction API v1");
        c.OAuthClientId("swagger");
        c.OAuthUsePkce();
        c.OAuthScopes("openid", "profile", "roles", "auctions.read", "auctions.write", "artauction.fullaccess");
        c.OAuthAppName("Swagger UI");
        
        // Persist authorization data in Swagger UI
        c.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Authentication and Authorization middleware
app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();

// Lab #6: Map gRPC Services
app.MapGrpcService<AuctionGrpcService>();

app.Run();
