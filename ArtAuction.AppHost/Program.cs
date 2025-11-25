var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - Databases & Cache
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("mongodb-data") // Persistent storage
    .WithMongoExpress(); // MongoDB admin UI

var artauctiondb = mongodb.AddDatabase("artauction-db");

// Redis for distributed caching
var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data") // Persistent cache storage
    .WithRedisCommander(); // Redis admin UI

// Microservices
var webApi = builder.AddProject<Projects.ArtAuction_WebApi>("artauction-webapi")
    .WithReference(artauctiondb)
    .WithReference(redis); // Redis connection for caching

// Gateway - Entry point
var apiGateway = builder.AddProject<Projects.ArtAuction_ApiGateway>("api-gateway")
    .WithReference(webApi)
    .WithReference(redis) // Redis for gateway caching
    .WithExternalHttpEndpoints(); // Expose to outside

// Aggregator - Composite data service
var aggregator = builder.AddProject<Projects.ArtAuction_Aggregator>("aggregator")
    .WithReference(webApi)
    .WithReference(redis); // Redis for aggregated results caching

builder.Build().Run();
