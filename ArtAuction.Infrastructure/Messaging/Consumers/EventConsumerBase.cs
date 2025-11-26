using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ArtAuction.Infrastructure.Messaging.Consumers;

public abstract class EventConsumerBase : BackgroundService
{
    private readonly IConnection _connection;
    protected readonly ILogger Logger;
    private readonly string _queueName;
    private IChannel? _channel;

    protected EventConsumerBase(
        IConnection connection,
        ILogger logger,
        string queueName)
    {
        _connection = connection;
        Logger = logger;
        _queueName = queueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Configure QoS - prefetch 10 messages at a time
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var activityName = $"Process {_queueName}";
            using var activity = new Activity(activityName);

            try
            {
                // Restore trace context from message headers
                if (ea.BasicProperties.Headers?.TryGetValue("traceparent", out var traceparent) == true)
                {
                    var traceparentString = Encoding.UTF8.GetString((byte[])traceparent);
                    activity.SetParentId(traceparentString);
                }

                activity.Start();

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var correlationId = ea.BasicProperties.CorrelationId;
                var eventType = ea.BasicProperties.Headers?.TryGetValue("event-type", out var et) == true 
                    ? Encoding.UTF8.GetString((byte[])et) 
                    : "Unknown";

                activity.SetTag("messaging.system", "rabbitmq");
                activity.SetTag("messaging.destination", _queueName);
                activity.SetTag("messaging.operation", "process");
                activity.SetTag("messaging.message_id", ea.DeliveryTag.ToString());
                activity.SetTag("event.type", eventType);
                activity.SetTag("event.correlation_id", correlationId);

                Logger.LogInformation(
                    "Received event {EventType} from queue {Queue}, CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                    eventType, _queueName, correlationId, activity.TraceId);

                // Process message
                var success = await ProcessMessageAsync(message, eventType, correlationId, stoppingToken);

                if (success)
                {
                    // Acknowledge successful processing
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    Logger.LogInformation("Message processed successfully and acknowledged");
                }
                else
                {
                    // Requeue for retry or send to DLQ based on retry count
                    var retryCount = GetRetryCount(ea.BasicProperties);
                    if (retryCount < 3)
                    {
                        // Requeue with incremented retry counter
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                        Logger.LogWarning("Message processing failed, requeuing (retry {RetryCount}/3)", retryCount + 1);
                    }
                    else
                    {
                        // Send to Dead Letter Queue after max retries
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                        Logger.LogError("Message processing failed after {RetryCount} retries, sending to DLQ", retryCount);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing message from queue {Queue}", _queueName);
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false, // Manual acknowledgment
            consumer: consumer,
            cancellationToken: stoppingToken);

        Logger.LogInformation("Event consumer started for queue {Queue}", _queueName);

        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    protected abstract Task<bool> ProcessMessageAsync(string message, string eventType, string correlationId, CancellationToken cancellationToken);

    private int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers?.TryGetValue("x-retry-count", out var count) == true)
        {
            return Convert.ToInt32(count);
        }
        return 0;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Event consumer stopping for queue {Queue}", _queueName);
        
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
