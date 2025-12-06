using ArtAuction.IdentityServer;
using ArtAuction.IdentityServer.Data;
using ArtAuction.IdentityServer.Services;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add SQL Server databases from Aspire with retry logic
builder.AddSqlServerDbContext<ApplicationDbContext>("ApplicationDb", settings =>
{
    settings.CommandTimeout = 120;
}, configureDbContextOptions: options =>
{
    options.UseSqlServer(o =>
    {
        o.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// Configure ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure IdentityServer
var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
var connectionString = builder.Configuration.GetConnectionString("ConfigurationDb") ?? 
    "Server=localhost;Database=ArtAuction.IdentityServer.Configuration;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // See https://docs.duendesoftware.com/identityserver/v7/fundamentals/resources/
    options.EmitStaticAudienceClaim = true;
})
.AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(
        builder.Configuration.GetConnectionString("ConfigurationDb") ?? connectionString,
        sql =>
        {
            sql.MigrationsAssembly(migrationsAssembly);
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
})
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(
        builder.Configuration.GetConnectionString("PersistedGrantDb") ?? 
        "Server=localhost;Database=ArtAuction.IdentityServer.PersistedGrant;Trusted_Connection=True;TrustServerCertificate=True",
        sql =>
        {
            sql.MigrationsAssembly(migrationsAssembly);
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

    // Automatic cleanup of old tokens
    options.EnableTokenCleanup = true;
    options.TokenCleanupInterval = 3600; // 1 hour
})
.AddAspNetIdentity<ApplicationUser>() // Integrate with ASP.NET Identity
.AddProfileService<ProfileService>() // Add custom profile service for role claims
.AddDeveloperSigningCredential(); // Use development signing certificate (NOT FOR PRODUCTION!)

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://localhost:7270",
                "https://localhost:7019",
                "https://localhost:7001",
                "https://localhost:5001",
                "https://localhost:3000",
                "http://localhost:5146",   // Aspire dynamic port HTTP
                "https://localhost:5146")  // Aspire dynamic port HTTPS
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add services for controllers and Razor pages (for Quickstart UI)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.MapDefaultEndpoints();

// Initialize database in background (non-blocking)
_ = Task.Run(async () =>
{
    // Wait longer for SQL Server container to be ready
    await Task.Delay(TimeSpan.FromSeconds(15));
    
    // Retry initialization multiple times
    for (int i = 0; i < 10; i++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await InitializeDatabaseAsync(scope.ServiceProvider);
            break; // Success - exit loop
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database initialization attempt {Attempt} failed. Retrying in 10 seconds...", i + 1);
            
            if (i < 9) // Don't wait after last attempt
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(); // CORS before authentication

app.UseIdentityServer(); // Middleware for IdentityServer
app.UseAuthorization();

// Log signing keys after IdentityServer middleware is configured
var logger = app.Services.GetRequiredService<ILogger<Program>>();
_ = Task.Run(async () =>
{
    await Task.Delay(5000); // Wait for IdentityServer to initialize
    try
    {
        logger.LogInformation("Testing IdentityServer endpoints...");
        
        // Test discovery document
        var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        
        var discoveryUrl = "https://localhost:7254/.well-known/openid-configuration";
        var response = await httpClient.GetAsync(discoveryUrl);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("✓ Discovery document accessible. Length: {Length} bytes", content.Length);
            
            // Parse to get jwks_uri
            var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("jwks_uri", out var jwksUriElement))
            {
                var jwksUri = jwksUriElement.GetString();
                logger.LogInformation("JWKS URI from discovery: {JwksUri}", jwksUri);
                
                // Try to fetch JWKS
                var jwksResponse = await httpClient.GetAsync(jwksUri);
                if (jwksResponse.IsSuccessStatusCode)
                {
                    var jwksContent = await jwksResponse.Content.ReadAsStringAsync();
                    logger.LogInformation("✓ JWKS endpoint accessible. Content: {Content}", jwksContent);
                }
                else
                {
                    logger.LogError("✗ JWKS endpoint returned {Status}", jwksResponse.StatusCode);
                }
            }
            else
            {
                logger.LogError("✗ Discovery document does not contain jwks_uri!");
            }
        }
        else
        {
            logger.LogError("✗ Discovery document returned {Status}", response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error checking IdentityServer configuration");
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

async Task InitializeDatabaseAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database initialization...");
        
        // Apply migrations for ApplicationDb
        var applicationDbContext = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Applying ApplicationDb migrations...");
        await applicationDbContext.Database.MigrateAsync();

        // Apply migrations for ConfigurationDb
        var configurationDbContext = services.GetRequiredService<ConfigurationDbContext>();
        logger.LogInformation("Applying ConfigurationDb migrations...");
        await configurationDbContext.Database.MigrateAsync();

        // Seed configuration data (Clients, Resources, Scopes)
        logger.LogInformation("Seeding configuration data...");
        
        // TEMPORARY: Force reload of client configuration to pick up new redirect URIs
        var existingClients = await configurationDbContext.Clients
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedCorsOrigins)
            .ToListAsync();
        
        if (existingClients.Any())
        {
            logger.LogInformation("Removing existing clients to reload configuration...");
            configurationDbContext.Clients.RemoveRange(existingClients);
            await configurationDbContext.SaveChangesAsync();
        }
        
        // Always seed clients (after removal or if empty)
        logger.LogInformation("Seeding clients...");
        foreach (var client in Config.Clients)
        {
            configurationDbContext.Clients.Add(client.ToEntity());
        }
        await configurationDbContext.SaveChangesAsync();

        if (!configurationDbContext.IdentityResources.Any())
        {
            foreach (var resource in Config.IdentityResources)
            {
                configurationDbContext.IdentityResources.Add(resource.ToEntity());
            }
            await configurationDbContext.SaveChangesAsync();
        }

        if (!configurationDbContext.ApiScopes.Any())
        {
            foreach (var apiScope in Config.ApiScopes)
            {
                configurationDbContext.ApiScopes.Add(apiScope.ToEntity());
            }
            await configurationDbContext.SaveChangesAsync();
        }

        if (!configurationDbContext.ApiResources.Any())
        {
            foreach (var resource in Config.ApiResources)
            {
                configurationDbContext.ApiResources.Add(resource.ToEntity());
            }
            await configurationDbContext.SaveChangesAsync();
        }

        // Apply migrations for PersistedGrantDb
        var persistedGrantDbContext = services.GetRequiredService<PersistedGrantDbContext>();
        logger.LogInformation("Applying PersistedGrantDb migrations...");
        await persistedGrantDbContext.Database.MigrateAsync();

        // Seed default roles and users
        logger.LogInformation("Seeding roles and users...");
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create default roles
        string[] roleNames = { "Admin", "User", "AuctionManager" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create default admin user
        var adminEmail = "admin@artauction.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Create test user
        var testEmail = "test@artauction.com";
        var testUser = await userManager.FindByEmailAsync(testEmail);
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(testUser, "Test@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, "User");
            }
        }
        
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
        // Don't throw - let the app continue running
    }
}
