using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Infrastructure.Repositories;

namespace ArtAuction.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB Configuration
        var mongoConnectionString = configuration.GetConnectionString("artauction-db") 
            ?? configuration.GetConnectionString("MongoDb")
            ?? "mongodb://localhost:27017";
        
        var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);
        var mongoClient = new MongoClient(mongoSettings);
        var database = mongoClient.GetDatabase("artauction");

        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(database);

        // Register repositories
        services.AddScoped<IAuctionRepository, MongoAuctionRepository>();

        return services;
    }
}
