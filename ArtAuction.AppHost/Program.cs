var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - Databases
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("mongodb-data") // Persistent storage
    .WithMongoExpress(); // MongoDB admin UI

var artauctiondb = mongodb.AddDatabase("artauction-db");

// Microservices
var webApi = builder.AddProject<Projects.ArtAuction_WebApi>("artauction-webapi")
    .WithReference(artauctiondb);

// Gateway - Entry point
var apiGateway = builder.AddProject<Projects.ArtAuction_ApiGateway>("api-gateway")
    .WithReference(webApi)
    .WithExternalHttpEndpoints(); // Expose to outside

// Aggregator - Composite data service
var aggregator = builder.AddProject<Projects.ArtAuction_Aggregator>("aggregator")
    .WithReference(webApi);

builder.Build().Run();
