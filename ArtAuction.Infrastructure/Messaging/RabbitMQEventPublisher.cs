using ArtAuction.Application.Common.Interfaces;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ArtAuction.Infrastructure.Messaging;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMQEventPublisher(IConnection connection, ILogger<RabbitMQEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default) 
        where TEvent : class
    {
        using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        try
        {
            // Serialize event to JSON
            var json = JsonSerializer.Serialize(@event, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            // Prepare message properties
            var properties = new BasicProperties
            {
                Persistent = true, // Persistent delivery
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>()
            };

            // Add correlation ID and trace context for distributed tracing
            var eventType = typeof(TEvent).Name;
            var correlationId = GetCorrelationId(@event);
            var activity = Activity.Current;

            properties.CorrelationId = correlationId;
            properties.Headers["event-type"] = eventType;
            properties.Headers["event-version"] = "1.0";
            
            // Propagate trace context (W3C traceparent)
            if (activity != null)
            {
                properties.Headers["traceparent"] = activity.Id;
                properties.Headers["tracestate"] = activity.TraceStateString ?? string.Empty;
            }

            // Publish to exchange
            const string exchangeName = "auction-events";
            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} with routing key {RoutingKey}, CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                eventType, routingKey, correlationId, activity?.TraceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} with routing key {RoutingKey}", 
                typeof(TEvent).Name, routingKey);
            throw;
        }
    }

    private string GetCorrelationId<TEvent>(TEvent @event) where TEvent : class
    {
        // Try to get CorrelationId from event using reflection
        var prop = typeof(TEvent).GetProperty("CorrelationId");
        var value = prop?.GetValue(@event) as string;
        return !string.IsNullOrEmpty(value) ? value : Guid.NewGuid().ToString();
    }
}
