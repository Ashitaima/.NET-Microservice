using Duende.IdentityServer.Models;

namespace ArtAuction.IdentityServer;

/// <summary>
/// IdentityServer configuration for clients, API resources, identity resources, and scopes
/// </summary>
public static class Config
{
    /// <summary>
    /// Identity resources (user claims like profile, email, etc.)
    /// </summary>
    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResource
            {
                Name = "roles",
                DisplayName = "User roles",
                UserClaims = new[] { "role" }
            }
        };

    /// <summary>
    /// API scopes (fine-grained permissions)
    /// </summary>
    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            // Auction API scopes
            new ApiScope("auctions.read", "Read access to auction API"),
            new ApiScope("auctions.write", "Write access to auction API"),
            
            // Order API scopes (future)
            new ApiScope("orders.read", "Read access to order API"),
            new ApiScope("orders.write", "Write access to order API"),
            
            // Admin scope
            new ApiScope("admin", "Admin access to all APIs"),
            
            // Full API access scope
            new ApiScope("artauction.fullaccess", "Full access to ArtAuction APIs")
        };

    /// <summary>
    /// API resources (logical grouping of scopes)
    /// </summary>
    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new ApiResource("auctions_api", "ArtAuction Auctions API")
            {
                Scopes = { "auctions.read", "auctions.write" },
                UserClaims = { "role", "email", "name" }
            },
            new ApiResource("orders_api", "ArtAuction Orders API")
            {
                Scopes = { "orders.read", "orders.write" },
                UserClaims = { "role", "email", "name" }
            },
            new ApiResource("artauction_api", "ArtAuction Full API")
            {
                Scopes = { "artauction.fullaccess", "admin" },
                UserClaims = { "role", "email", "name", "sub" }
            }
        };

    /// <summary>
    /// Clients (applications that can request tokens)
    /// </summary>
    public static IEnumerable<Client> Clients =>
        new List<Client>
        {
            // Postman client for Authorization Code + PKCE flow
            new Client
            {
                ClientId = "postman",
                ClientName = "Postman Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false, // Public client
                
                RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                PostLogoutRedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                AllowedCorsOrigins = { "https://oauth.pstmn.io" },
                
                AllowedScopes = 
                { 
                    "openid", 
                    "profile", 
                    "email", 
                    "roles",
                    "auctions.read", 
                    "auctions.write",
                    "artauction.fullaccess"
                },
                
                AllowOfflineAccess = true, // Refresh tokens
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 7 * 24 * 3600, // 7 days
                AccessTokenLifetime = 3600 // 1 hour
            },

            // Swagger UI client for interactive API documentation
            new Client
            {
                ClientId = "swagger",
                ClientName = "Swagger UI",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                
                RedirectUris = 
                { 
                    "https://localhost:7270/swagger/oauth2-redirect.html", // WebAPI
                    "https://localhost:7019/swagger/oauth2-redirect.html", // Gateway
                    "https://localhost:7001/swagger/oauth2-redirect.html",
                    "https://localhost:5001/swagger/oauth2-redirect.html",
                    "https://localhost:7254/swagger/oauth2-redirect.html", // IdentityServer
                    "http://localhost:5146/swagger/oauth2-redirect.html",  // Aspire dynamic port
                    "https://localhost:5146/swagger/oauth2-redirect.html"  // Aspire dynamic port (HTTPS)
                },
                PostLogoutRedirectUris = 
                { 
                    "https://localhost:7270/swagger",
                    "https://localhost:7019/swagger",
                    "https://localhost:7001/swagger",
                    "https://localhost:5001/swagger",
                    "http://localhost:5146/swagger",
                    "https://localhost:5146/swagger"
                },
                AllowedCorsOrigins = 
                { 
                    "https://localhost:7270",
                    "https://localhost:7019",
                    "https://localhost:7001",
                    "https://localhost:5001",
                    "https://localhost:7254",
                    "http://localhost:5146",
                    "https://localhost:5146"
                },
                
                AllowedScopes = 
                { 
                    "openid", 
                    "profile", 
                    "email", 
                    "roles",
                    "auctions.read", 
                    "auctions.write",
                    "artauction.fullaccess"
                },
                
                AllowAccessTokensViaBrowser = true,
                RequireConsent = false, // Skip consent screen for development
                AccessTokenLifetime = 3600
            },

            // API Client for Client Credentials flow (direct API testing)
            new Client
            {
                ClientId = "artauction_api",
                ClientName = "ArtAuction API Client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                
                ClientSecrets = 
                { 
                    new Secret("artauction-secret-2024".Sha256()) 
                },
                
                AllowedScopes = 
                { 
                    "auctions.read", 
                    "auctions.write",
                    "artauction.fullaccess"
                },
                
                AccessTokenLifetime = 3600
            },

            // Aggregator service client for machine-to-machine communication
            new Client
            {
                ClientId = "aggregator_service",
                ClientName = "Aggregator Service",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                
                ClientSecrets = 
                { 
                    new Secret("aggregator_secret_key_change_in_production".Sha256()) 
                },
                
                AllowedScopes = 
                { 
                    "auctions.read", 
                    "orders.read",
                    "artauction.fullaccess"
                },
                
                AccessTokenLifetime = 3600
            },

            // Web Client (future frontend application)
            new Client
            {
                ClientId = "web_client",
                ClientName = "ArtAuction Web Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                
                RedirectUris = { "https://localhost:3000/callback" },
                PostLogoutRedirectUris = { "https://localhost:3000" },
                AllowedCorsOrigins = { "https://localhost:3000" },
                
                AllowedScopes = 
                { 
                    "openid", 
                    "profile", 
                    "email", 
                    "roles",
                    "auctions.read", 
                    "auctions.write",
                    "orders.read",
                    "orders.write"
                },
                
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 30 * 24 * 3600, // 30 days
                AccessTokenLifetime = 900 // 15 minutes
            }
        };
}
