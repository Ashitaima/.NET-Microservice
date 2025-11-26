using MongoDB.Driver;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Infrastructure.Repositories;
using ArtAuction.Infrastructure.Messaging;
using ArtAuction.Infrastructure.Messaging.Consumers;

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

        // RabbitMQ Configuration - Optional for development
        services.AddSingleton<IConnection>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("RabbitMQ");
            var connectionString = configuration.GetConnectionString("rabbitmq") 
                ?? "amqp://guest:guest@localhost:5672";
            
            try
            {
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(connectionString);
                factory.ClientProvidedName = "ArtAuction.WebApi";
                factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(5);
                factory.ContinuationTimeout = TimeSpan.FromSeconds(5);
                
                var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                logger.LogInformation("RabbitMQ connection established to {Uri}", connectionString);
                
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect to RabbitMQ at {Uri}. Application will continue without RabbitMQ.", connectionString);
                // Return null - services will handle gracefully
                return null!;
            }
        });

        // Register event publisher
        services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();

        // Register topology initializer as hosted service
        services.AddHostedService<RabbitMQTopologyInitializer>();

        // Register event consumers as hosted services
        services.AddHostedService<AuctionEventsConsumer>();
        services.AddHostedService<BidEventsConsumer>();

        return services;
    }
}
