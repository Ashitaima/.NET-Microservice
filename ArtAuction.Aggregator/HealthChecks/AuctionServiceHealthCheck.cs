using ArtAuction.Aggregator.Protos;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArtAuction.Aggregator.HealthChecks;

/// <summary>
/// Lab #7: Custom health check для downstream WebApi gRPC service
/// </summary>
public class AuctionServiceHealthCheck : IHealthCheck
{
    private readonly AuctionService.AuctionServiceClient _client;
    private readonly ILogger<AuctionServiceHealthCheck> _logger;

    public AuctionServiceHealthCheck(
        AuctionService.AuctionServiceClient client,
        ILogger<AuctionServiceHealthCheck> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create linked cancellation token with 2-second timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // Lightweight health check - try to get single auction
            // In production, use dedicated Health gRPC endpoint if available
            var response = await _client.GetAuctionAsync(
                new GetAuctionRequest { Id = "health-check" },
                cancellationToken: cts.Token);

            _logger.LogInformation("AuctionService health check successful");
            return HealthCheckResult.Healthy("AuctionService is responsive");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogWarning("AuctionService unavailable: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy("AuctionService unavailable", ex);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogWarning("AuctionService timeout: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy("AuctionService timeout", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("AuctionService health check timeout");
            return HealthCheckResult.Unhealthy("AuctionService health check timeout", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuctionService health check failed");
            return HealthCheckResult.Unhealthy("AuctionService check failed", ex);
        }
    }
}
