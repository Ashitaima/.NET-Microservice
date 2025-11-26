using ArtAuction.Aggregator.Models;
using ArtAuction.Aggregator.Protos;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;

namespace ArtAuction.Aggregator.Services;

/// <summary>
/// Aggregator service that combines data from multiple microservices using gRPC
/// with parallel calls and distributed caching
/// </summary>
public class AuctionAggregatorService
{
    private readonly AuctionService.AuctionServiceClient _auctionClient;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AuctionAggregatorService> _logger;

    public AuctionAggregatorService(
        AuctionService.AuctionServiceClient auctionClient,
        IDistributedCache distributedCache,
        ILogger<AuctionAggregatorService> logger)
    {
        _auctionClient = auctionClient;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// Get aggregated auction details with parallel gRPC calls
    /// </summary>
    public async Task<AggregatedAuctionDto?> GetAggregatedAuctionAsync(
        string auctionId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"aggregated:auction:{auctionId}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Lab #7: Graceful degradation - try cache but continue without it if unavailable
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Cache HIT (Aggregated): {CacheKey}, latency: {Latency}ms", 
                        cacheKey, stopwatch.ElapsedMilliseconds);
                    return JsonSerializer.Deserialize<AggregatedAuctionDto>(cachedData);
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Cache read failed for {CacheKey}, continuing without cache", cacheKey);
                // Continue without cache - graceful degradation
            }

            _logger.LogInformation("Cache MISS (Aggregated): {CacheKey}, making parallel gRPC calls...", cacheKey);

            // Parallel gRPC calls to different services
            var auctionCall = _auctionClient.GetAuctionAsync(
                new GetAuctionRequest { Id = auctionId }, 
                cancellationToken: cancellationToken);

            // Simulate additional service calls (users, analytics, etc.)
            var additionalDataTask = GetMockAdditionalDataAsync(auctionId, cancellationToken);

            // Wait for all parallel calls with partial failure handling
            var results = await Task.WhenAll(
                SafeAwait(auctionCall.ResponseAsync, "AuctionService"),
                SafeAwait(additionalDataTask, "AdditionalData")
            );

            var auctionResponse = results[0] as AuctionResponse;
            if (auctionResponse == null)
            {
                _logger.LogWarning("Failed to get auction data for {AuctionId}", auctionId);
                return null;
            }

            // Aggregate results
            var aggregatedData = new AggregatedAuctionDto
            {
                Id = auctionResponse.Id,
                Title = auctionResponse.Title,
                Description = auctionResponse.Description,
                CurrentPrice = (decimal)auctionResponse.CurrentPrice.Amount,
                Currency = auctionResponse.CurrentPrice.Currency,
                Status = auctionResponse.Status,
                EndTime = new DateTime(auctionResponse.EndTimeTicks, DateTimeKind.Utc),
                TotalBids = auctionResponse.HighestBid != null ? 1 : 0,
                SellerInfo = new SellerInfoDto
                {
                    SellerId = auctionResponse.SellerId,
                    SellerName = "Seller Name", // Mock data
                    SellerRating = 4.5
                },
                HighestBidder = auctionResponse.HighestBid != null 
                    ? new HighestBidderInfoDto
                    {
                        BidderId = auctionResponse.HighestBid.BidderId,
                        BidderName = "Bidder Name", // Mock data
                        BidAmount = (decimal)auctionResponse.HighestBid.Amount.Amount,
                        BidTime = new DateTime(auctionResponse.HighestBid.TimestampTicks, DateTimeKind.Utc)
                    }
                    : null,
                AggregatedAt = DateTime.UtcNow,
                SourceServices = new List<string> { "AuctionService", "UserService" }
            };

            // Lab #7: Cache write with graceful degradation (best effort)
            try
            {
                var serialized = JsonSerializer.Serialize(aggregatedData);
                await _distributedCache.SetStringAsync(
                    cacheKey,
                    serialized,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                    },
                    cancellationToken);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Cache write failed for {CacheKey}, continuing without caching", cacheKey);
                // Ignore cache write failures - continue successfully
            }

            stopwatch.Stop();
            _logger.LogInformation("Aggregated auction {AuctionId} in {Latency}ms", 
                auctionId, stopwatch.ElapsedMilliseconds);

            return aggregatedData;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while aggregating auction {AuctionId}: {Status}", 
                auctionId, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating auction {AuctionId}", auctionId);
            throw;
        }
    }

    /// <summary>
    /// Get aggregated auctions list with parallel gRPC calls
    /// </summary>
    public async Task<AggregatedAuctionsListDto> GetAggregatedAuctionsAsync(
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"aggregated:auctions:page:{pageNumber}:size:{pageSize}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Lab #7: Graceful degradation for cache read
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Cache HIT (Aggregated List): page {Page}", pageNumber);
                    return JsonSerializer.Deserialize<AggregatedAuctionsListDto>(cachedData)!;
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Cache read failed for page {Page}, continuing without cache", pageNumber);
            }

            _logger.LogInformation("Cache MISS (Aggregated List): page {Page}, making parallel gRPC calls...", pageNumber);

            // Parallel calls
            var auctionsCall = _auctionClient.GetAuctionsAsync(
                new GetAuctionsRequest { PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken: cancellationToken);

            var activeAuctionsCall = _auctionClient.GetActiveAuctionsAsync(
                new GetActiveAuctionsRequest { PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken: cancellationToken);

            // Wait for all with partial failure handling
            await Task.WhenAll(
                SafeAwait(auctionsCall.ResponseAsync, "GetAuctions"),
                SafeAwait(activeAuctionsCall.ResponseAsync, "GetActiveAuctions")
            );

            var auctionsResponse = await auctionsCall.ResponseAsync;

            // Map to aggregated DTOs
            var aggregatedAuctions = auctionsResponse.Auctions.Select(a => new AggregatedAuctionDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                CurrentPrice = (decimal)a.CurrentPrice.Amount,
                Currency = a.CurrentPrice.Currency,
                Status = a.Status,
                EndTime = new DateTime(a.EndTimeTicks, DateTimeKind.Utc),
                TotalBids = a.HighestBid != null ? 1 : 0,
                AggregatedAt = DateTime.UtcNow,
                SourceServices = new List<string> { "AuctionService" }
            }).ToList();

            stopwatch.Stop();

            var result = new AggregatedAuctionsListDto
            {
                Auctions = aggregatedAuctions,
                TotalCount = auctionsResponse.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                AggregatedAt = DateTime.UtcNow,
                ProcessingTime = stopwatch.Elapsed
            };

            // Lab #7: Cache write with graceful degradation
            try
            {
                var serialized = JsonSerializer.Serialize(result);
                await _distributedCache.SetStringAsync(
                    cacheKey,
                    serialized,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                    },
                    cancellationToken);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Cache write failed for page {Page}, continuing without caching", pageNumber);
            }

            _logger.LogInformation("Aggregated {Count} auctions in {Latency}ms",
                aggregatedAuctions.Count, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating auctions list page {Page}", pageNumber);
            throw;
        }
    }

    /// <summary>
    /// Partial failure handling - returns null if task fails instead of throwing
    /// </summary>
    private async Task<object?> SafeAwait(Task task, string serviceName)
    {
        try
        {
            await task;
            
            // Extract result from completed task
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        catch (RpcException ex)
        {
            _logger.LogWarning("Partial failure in {Service}: {Status} - {Message}", 
                serviceName, ex.StatusCode, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Partial failure in {Service}", serviceName);
            return null;
        }
    }

    /// <summary>
    /// Mock method simulating additional service call
    /// </summary>
    private async Task<object> GetMockAdditionalDataAsync(string auctionId, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulate network delay
        return new { Analytics = "Mock analytics data", Views = 123 };
    }
}
