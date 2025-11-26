var builder = DistributedApplication.CreateBuilder(args);

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

// Microservices
var webApi = builder.AddProject<Projects.ArtAuction_WebApi>("artauction-webapi")
    .WithReference(artauctiondb) // MongoDB connection
    .WithReference(redis) // Redis connection for caching
    .WithReference(rabbitmq); // RabbitMQ for events

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
