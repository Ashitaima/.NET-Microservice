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

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults - OpenTelemetry, Serilog, Health Checks
builder.AddServiceDefaults();

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("IdentityServer", options =>
{
    options.Authority = identityServerUrl;
    options.RequireHttpsMetadata = false; // For dev only
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidAudiences = new[] { "artauction_api", "auctions_api" },
        ValidateIssuer = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
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

// Set policy to accept any of the three schemes
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, "IdentityServer or Keycloak or Legacy", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            {
                var token = authorization.Substring("Bearer ".Length).Trim();
                
                // Keycloak tokens typically contain "realm_access" claim
                // IdentityServer tokens are typically longer and contain "client_id"
                // Legacy tokens are shorter
                if (token.Length < 400)
                {
                    return "Legacy";
                }
                // Try Keycloak first, fallback to IdentityServer
                // Both will validate against their respective authorities
                return "Keycloak"; // Will auto-fallback if validation fails
            }
            return "IdentityServer";
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
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{identityServerUrl}/connect/authorize"),
                TokenUrl = new Uri($"{identityServerUrl}/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" },
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
            new[] { "openid", "profile", "auctions.read", "auctions.write", "artauction.fullaccess" }
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
        c.OAuthScopes("openid", "profile", "auctions.read", "auctions.write", "artauction.fullaccess");
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
