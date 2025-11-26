var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - PostgreSQL for Keycloak (Lab #9 Part 3)
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

var keycloakDb = postgres.AddDatabase("keycloak-db");

// Infrastructure - Keycloak for OAuth 2.0 / OpenID Connect (Lab #9 Part 3)
// Keycloak needs JDBC URL format, so we build it from PostgreSQL connection info
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.4.6")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "management")
    .WithBindMount("keycloak-data", "/opt/keycloak/data")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin123456")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL_HOST", postgres.Resource.Name)
    .WithEnvironment("KC_DB_URL_PORT", "5432")
    .WithEnvironment("KC_DB_URL_DATABASE", "keycloak-db")
    .WithEnvironment("KC_DB_USERNAME", "postgres")
    .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KC_METRICS_ENABLED", "true")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEnvironment("KC_HOSTNAME_STRICT_HTTPS", "false")
    .WithArgs("start")
    .WaitFor(keycloakDb); // Wait for PostgreSQL to be ready

// Infrastructure - MongoDB for NoSQL data storage (Lab #4)
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("mongodb-data"); // Persistent storage

var artauctiondb = mongodb.AddDatabase("artauction-db");

// Infrastructure - Redis for distributed caching
var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data") // Persistent cache storage
    .WithRedisCommander(); // Redis admin UI

// Infrastructure - RabbitMQ for async event-driven messaging (Lab #8)
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("rabbitmq-data") // Persistent message storage
    .WithManagementPlugin(); // Management UI on port 15672

// Infrastructure - SQL Server for Identity data (Lab #9)
var identitySql = builder.AddSqlServer("identity-sql")
    .WithDataVolume("identity-sql-data");

var identityDb = identitySql.AddDatabase("IdentityDb");

// Infrastructure - SQL Server for IdentityServer (Lab #9 Part 2)
var identityServerSql = builder.AddSqlServer("identityserver-sql")
    .WithDataVolume("identityserver-sql-data");

var configurationDb = identityServerSql.AddDatabase("ConfigurationDb");
var persistedGrantDb = identityServerSql.AddDatabase("PersistedGrantDb");
var applicationDb = identityServerSql.AddDatabase("ApplicationDb");

// Microservices - Auth Service for Authentication/Authorization (Lab #9)
var authService = builder.AddProject<Projects.ArtAuction_Auth_Service>("auth-service")
    .WithReference(identityDb)
    .WithEnvironment("DCP_IDE_REQUEST_TIMEOUT_SECONDS", "180"); // Increase timeout for DB migrations

// Microservices - IdentityServer for OAuth 2.0 / OpenID Connect (Lab #9 Part 2)
var identityServer = builder.AddProject<Projects.ArtAuction_IdentityServer>("identityserver")
    .WithReference(configurationDb)
    .WithReference(persistedGrantDb)
    .WithReference(applicationDb)
    .WithEnvironment("DCP_IDE_REQUEST_TIMEOUT_SECONDS", "180"); // Increase timeout for DB migrations

// Microservices
var webApi = builder.AddProject<Projects.ArtAuction_WebApi>("artauction-webapi")
    .WithReference(artauctiondb) // MongoDB connection
    .WithReference(redis) // Redis connection for caching
    .WithReference(rabbitmq) // RabbitMQ for events
    .WithReference(identityServer) // IdentityServer for OAuth 2.0
    .WithEnvironment("ConnectionStrings__keycloak", "http://keycloak:8080"); // Keycloak URL

// Gateway - Entry point
var apiGateway = builder.AddProject<Projects.ArtAuction_ApiGateway>("api-gateway")
    .WithReference(authService)
    .WithReference(webApi)
    .WithReference(identityServer) // IdentityServer for OAuth proxy
    .WithReference(redis) // Redis for gateway caching
    .WithEnvironment("ReverseProxy__Clusters__keycloak-cluster__Destinations__destination1__Address", "http://keycloak:8080")
    .WithExternalHttpEndpoints(); // Expose to outside

// Aggregator - Composite data service
var aggregator = builder.AddProject<Projects.ArtAuction_Aggregator>("aggregator")
    .WithReference(webApi)
    .WithReference(identityServer) // IdentityServer for M2M authentication
    .WithReference(redis); // Redis for aggregated results caching

builder.Build().Run();
