using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using ArtAuction.Domain.Events;

namespace ArtAuction.Infrastructure.Messaging.Consumers;

public class AuctionEventsConsumer : EventConsumerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuctionEventsConsumer(
        IConnection connection,
        ILogger<AuctionEventsConsumer> logger,
        IServiceProvider serviceProvider)
        : base(connection, logger, "auction-created-events")
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected override async Task<bool> ProcessMessageAsync(
        string message,
        string eventType,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            switch (eventType)
            {
                case "AuctionCreatedEvent":
                    var auctionCreated = JsonSerializer.Deserialize<AuctionCreatedEvent>(message, _jsonOptions);
                    if (auctionCreated != null)
                    {
                        // Check idempotency - skip if already processed
                        var isProcessed = await CheckIfEventProcessed(auctionCreated.EventId, scope);
                        if (isProcessed)
                        {
                            Logger.LogInformation("Event {EventId} already processed, skipping", auctionCreated.EventId);
                            return true; // ACK to remove from queue
                        }

                        // Process event - example: update materialized view, send notifications, etc.
                        Logger.LogInformation(
                            "Processing AuctionCreated: {AuctionId}, {ArtworkName}, Seller: {SellerId}",
                            auctionCreated.AuctionId, auctionCreated.ArtworkName, auctionCreated.SellerId);

                        // Store event ID to prevent duplicate processing
                        await MarkEventAsProcessed(auctionCreated.EventId, scope, cancellationToken);
                        
                        return true;
                    }
                    break;

                default:
                    Logger.LogWarning("Unknown event type: {EventType}", eventType);
                    return false;
            }

            return false;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to deserialize event {EventType}", eventType);
            return false; // Deserialization errors are permanent failures
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing event {EventType}", eventType);
            return false; // Retry for other errors
        }
    }

    private async Task<bool> CheckIfEventProcessed(Guid eventId, IServiceScope scope)
    {
        // TODO: Implement actual DB check
        // For now, return false (not processed)
        await Task.CompletedTask;
        return false;
    }

    private async Task MarkEventAsProcessed(Guid eventId, IServiceScope scope, CancellationToken cancellationToken)
    {
        // TODO: Implement actual DB storage of processed event IDs
        // This prevents duplicate processing (idempotency)
        await Task.CompletedTask;
        Logger.LogInformation("Marked event {EventId} as processed", eventId);
    }
}
