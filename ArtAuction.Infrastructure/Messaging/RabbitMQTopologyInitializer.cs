using RabbitMQ.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArtAuction.Infrastructure.Messaging;

public class RabbitMQTopologyInitializer : IHostedService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQTopologyInitializer> _logger;

    public RabbitMQTopologyInitializer(IConnection connection, ILogger<RabbitMQTopologyInitializer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        try
        {
            // Declare main events exchange (topic type for flexible routing)
            await channel.ExchangeDeclareAsync(
                exchange: "auction-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Declare dead letter exchange for failed messages
            await channel.ExchangeDeclareAsync(
                exchange: "auction-events-dlx",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Declare dead letter queue
            await channel.QueueDeclareAsync(
                queue: "auction-events-dlq",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Bind DLQ to DLX
            await channel.QueueBindAsync(
                queue: "auction-events-dlq",
                exchange: "auction-events-dlx",
                routingKey: "#",
                arguments: null,
                cancellationToken: cancellationToken);

            // Declare queues for different event types with DLX configuration
            var dlxArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", "auction-events-dlx" }
            };

            // Auction created events queue
            await channel.QueueDeclareAsync(
                queue: "auction-created-events",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: dlxArgs,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: "auction-created-events",
                exchange: "auction-events",
                routingKey: "auction.created",
                arguments: null,
                cancellationToken: cancellationToken);

            // Bid placed events queue
            await channel.QueueDeclareAsync(
                queue: "bid-placed-events",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: dlxArgs,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: "bid-placed-events",
                exchange: "auction-events",
                routingKey: "auction.bid.placed",
                arguments: null,
                cancellationToken: cancellationToken);

            // Auction finished events queue
            await channel.QueueDeclareAsync(
                queue: "auction-finished-events",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: dlxArgs,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: "auction-finished-events",
                exchange: "auction-events",
                routingKey: "auction.finished",
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ topology initialized successfully: exchanges, queues, and bindings created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ topology");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
