using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ArtAuction.Domain.Events;

namespace ArtAuction.Infrastructure.Messaging.Consumers;

public class BidEventsConsumer : EventConsumerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public BidEventsConsumer(
        IConnection connection,
        ILogger<BidEventsConsumer> logger,
        IServiceProvider serviceProvider)
        : base(connection, logger, "bid-placed-events")
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
            switch (eventType)
            {
                case "BidPlacedEvent":
                    var bidPlaced = JsonSerializer.Deserialize<BidPlacedEvent>(message, _jsonOptions);
                    if (bidPlaced != null)
                    {
                        // Process bid placed event
                        Logger.LogInformation(
                            "Processing BidPlaced: Auction {AuctionId}, Bidder: {BidderId}, Amount: {Amount}, TotalBids: {TotalBids}",
                            bidPlaced.AuctionId, bidPlaced.BidderId, bidPlaced.BidAmount, bidPlaced.TotalBids);

                        // Example use cases:
                        // - Invalidate cache for this auction
                        // - Send notification to previous bidder
                        // - Update leaderboard/statistics
                        // - Send real-time update via SignalR

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
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing event {EventType}", eventType);
            return false;
        }
    }
}
