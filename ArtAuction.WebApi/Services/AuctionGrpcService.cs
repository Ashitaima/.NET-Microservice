using ArtAuction.Domain.Entities;
using ArtAuction.Domain.ValueObjects;
using ArtAuction.Domain.Enums;
using ArtAuction.WebApi.Protos;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;

namespace ArtAuction.WebApi.Services;

/// <summary>
/// gRPC service implementation for Auction operations with multi-level caching
/// </summary>
public class AuctionGrpcService : Protos.AuctionService.AuctionServiceBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AuctionGrpcService> _logger;
    private readonly AuctionMetricsService _metrics;
    
    // TODO: Inject repository when Infrastructure layer is ready
    // private readonly IAuctionRepository _repository;

    public AuctionGrpcService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<AuctionGrpcService> logger,
        AuctionMetricsService metrics)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _metrics = metrics;
    }

    public override async Task<AuctionResponse> GetAuction(
        GetAuctionRequest request, 
        ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew(); // Lab #7: Measure latency
        var cacheKey = $"auction:{request.Id}";
        bool cacheHit = false;
        
        // L1 Cache: Check memory cache first
        if (_memoryCache.TryGetValue(cacheKey, out Auction? cachedAuction))
        {
            stopwatch.Stop();
            _logger.LogInformation("Cache HIT (L1 Memory): {CacheKey}", cacheKey);
            
            // Lab #7: Record metrics
            cacheHit = true;
            _metrics.RecordOperationDuration("GetAuction", stopwatch.Elapsed.TotalMilliseconds, "success", cacheHit);
            _metrics.RecordCacheDuration("get", stopwatch.Elapsed.TotalMilliseconds, "L1", true);
            
            return MapToAuctionResponse(cachedAuction!);
        }
        
        _logger.LogInformation("Cache MISS (L1): {CacheKey}, checking L2 (Redis)...", cacheKey);
        
        // L2 Cache: Check distributed cache (Redis)
        var distributedData = await _distributedCache.GetStringAsync(cacheKey, context.CancellationToken);
        if (!string.IsNullOrEmpty(distributedData))
        {
            stopwatch.Stop();
            _logger.LogInformation("Cache HIT (L2 Redis): {CacheKey}", cacheKey);
            var auction = System.Text.Json.JsonSerializer.Deserialize<Auction>(distributedData);
            
            // Lab #7: Record metrics
            cacheHit = true;
            _metrics.RecordOperationDuration("GetAuction", stopwatch.Elapsed.TotalMilliseconds, "success", cacheHit);
            _metrics.RecordCacheDuration("get", stopwatch.Elapsed.TotalMilliseconds, "L2", true);
            
            // Update L1 cache from L2
            _memoryCache.Set(cacheKey, auction, TimeSpan.FromMinutes(5)); // Shorter TTL for L1
            _metrics.IncrementCacheEntries();
            
            return MapToAuctionResponse(auction!);
        }
        
        _logger.LogInformation("Cache MISS (L2): {CacheKey}, querying database...", cacheKey);
        
        // Database query (simulated for now)
        // TODO: Replace with actual repository call
        var auctionFromDb = CreateMockAuction(request.Id);
        
        // Store in L2 cache (Redis) - longer TTL
        var serialized = System.Text.Json.JsonSerializer.Serialize(auctionFromDb);
        await _distributedCache.SetStringAsync(
            cacheKey, 
            serialized,
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) 
            },
            context.CancellationToken);
        
        // Store in L1 cache (Memory) - shorter TTL
        _memoryCache.Set(cacheKey, auctionFromDb, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        });
        
        _metrics.IncrementCacheEntries();
        _metrics.IncrementCacheEntries(); // Both L1 and L2
        
        stopwatch.Stop();
        _logger.LogInformation("Cached auction {AuctionId} in L1 and L2, latency: {Latency}ms", request.Id, stopwatch.Elapsed.TotalMilliseconds);
        
        // Lab #7: Record metrics for DB query
        _metrics.RecordOperationDuration("GetAuction", stopwatch.Elapsed.TotalMilliseconds, "success", cacheHit: false);
        
        return MapToAuctionResponse(auctionFromDb);
    }

    public override async Task<AuctionsListResponse> GetAuctions(
        GetAuctionsRequest request, 
        ServerCallContext context)
    {
        var cacheKey = $"auctions:page:{request.PageNumber}:size:{request.PageSize}";
        
        // Check memory cache
        if (_memoryCache.TryGetValue(cacheKey, out List<Auction>? cachedList))
        {
            _logger.LogInformation("Cache HIT (L1): Auctions list page {Page}", request.PageNumber);
            return MapToAuctionsListResponse(cachedList!, request.PageNumber, request.PageSize, cachedList!.Count);
        }
        
        _logger.LogInformation("Cache MISS: Auctions list page {Page}", request.PageNumber);
        
        // Simulate database query
        var auctions = CreateMockAuctionList(request.PageNumber, request.PageSize);
        
        // Cache with sliding expiration for frequently accessed pages
        _memoryCache.Set(cacheKey, auctions, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(3)
        });
        
        return MapToAuctionsListResponse(auctions, request.PageNumber, request.PageSize, auctions.Count);
    }

    public override async Task<AuctionResponse> CreateAuction(
        CreateAuctionRequest request, 
        ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew(); // Lab #7: Measure latency
        _logger.LogInformation("Creating new auction: {Title}", request.Title);
        
        // Use domain factory method to create auction
        var auction = Auction.Create(
            artworkName: request.Title,
            sellerId: request.SellerId,
            startPrice: (decimal)request.StartingPrice,
            startTime: DateTime.UtcNow,
            endTime: new DateTime(request.EndTimeTicks, DateTimeKind.Utc),
            description: request.Description
        );
        
        // TODO: Save to database via repository
        
        // Invalidate list caches after write operation
        InvalidateListCaches();
        
        stopwatch.Stop();
        _logger.LogInformation("Created auction, invalidated list caches, latency: {Latency}ms", stopwatch.Elapsed.TotalMilliseconds);
        
        // Lab #7: Record metrics
        _metrics.RecordAuctionCreated(auction.Status.ToString(), request.SellerId);
        _metrics.RecordOperationDuration("CreateAuction", stopwatch.Elapsed.TotalMilliseconds, "success", cacheHit: false);
        
        return MapToAuctionResponse(auction);
    }

    public override async Task<BidResponse> PlaceBid(
        PlaceBidRequest request, 
        ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew(); // Lab #7: Measure latency
        _logger.LogInformation("Placing bid on auction {AuctionId} by {BidderId}", 
            request.AuctionId, request.BidderId);
        
        var cacheKey = $"auction:{request.AuctionId}";
        
        // TODO: Load auction from database
        var auction = CreateMockAuction(request.AuctionId);
        
        try
        {
            // Use domain method to place bid
            var bidAmount = Money.Create((decimal)request.Amount);
            auction.PlaceBid(request.BidderId, bidAmount);
            
            // TODO: Save to database
            
            // Invalidate both L1 and L2 cache for this auction
            _memoryCache.Remove(cacheKey);
            await _distributedCache.RemoveAsync(cacheKey, context.CancellationToken);
            _metrics.DecrementCacheEntries();
            _metrics.DecrementCacheEntries(); // Both L1 and L2
            
            stopwatch.Stop();
            _logger.LogInformation("Invalidated cache for auction {AuctionId}, latency: {Latency}ms", 
                request.AuctionId, stopwatch.Elapsed.TotalMilliseconds);
            
            var latestBid = auction.Bids.Last();
            
            // Lab #7: Record successful bid metrics
            _metrics.RecordBidPlaced(request.AuctionId, request.BidderId, success: true);
            _metrics.RecordOperationDuration("PlaceBid", stopwatch.Elapsed.TotalMilliseconds, "success", cacheHit: false);
            
            return new BidResponse
            {
                Success = true,
                Message = "Bid placed successfully",
                Bid = MapToBidInfoMessage(latestBid)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to place bid on auction {AuctionId}, latency: {Latency}ms", 
                request.AuctionId, stopwatch.Elapsed.TotalMilliseconds);
            
            // Lab #7: Record failed bid metrics
            _metrics.RecordBidPlaced(request.AuctionId, request.BidderId, success: false);
            _metrics.RecordOperationDuration("PlaceBid", stopwatch.Elapsed.TotalMilliseconds, "failure", cacheHit: false);
            
            return new BidResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override async Task<AuctionsListResponse> GetActiveAuctions(
        GetActiveAuctionsRequest request, 
        ServerCallContext context)
    {
        var cacheKey = $"auctions:active:page:{request.PageNumber}:size:{request.PageSize}";
        
        if (_memoryCache.TryGetValue(cacheKey, out List<Auction>? cached))
        {
            _logger.LogInformation("Cache HIT: Active auctions page {Page}", request.PageNumber);
            return MapToAuctionsListResponse(cached!, request.PageNumber, request.PageSize, cached!.Count);
        }
        
        // Simulate active auctions query
        var activeAuctions = CreateMockAuctionList(request.PageNumber, request.PageSize)
            .Where(a => a.Status == AuctionStatus.Active)
            .ToList();
        
        // Cache with shorter TTL as active status changes frequently
        _memoryCache.Set(cacheKey, activeAuctions, TimeSpan.FromMinutes(2));
        
        return MapToAuctionsListResponse(activeAuctions, request.PageNumber, request.PageSize, activeAuctions.Count);
    }

    #region Mapping Methods
    
    private static AuctionResponse MapToAuctionResponse(Auction auction)
    {
        return new AuctionResponse
        {
            Id = auction.Id,
            Title = auction.ArtworkName,
            Description = auction.Description ?? string.Empty,
            StartingPrice = new MoneyMessage 
            { 
                Amount = (double)auction.StartPrice.Amount, 
                Currency = auction.StartPrice.Currency 
            },
            CurrentPrice = new MoneyMessage 
            { 
                Amount = (double)auction.CurrentPrice.Amount, 
                Currency = auction.CurrentPrice.Currency 
            },
            SellerId = auction.SellerId,
            StartTimeTicks = auction.StartTime.Ticks,
            EndTimeTicks = auction.EndTime.Ticks,
            Status = auction.Status.ToString(),
            HighestBid = auction.Bids.Count > 0 
                ? MapToBidInfoMessage(auction.Bids.OrderByDescending(b => b.Amount.Amount).First()) 
                : null
        };
    }

    private static AuctionsListResponse MapToAuctionsListResponse(
        List<Auction> auctions, 
        int pageNumber, 
        int pageSize, 
        int totalCount)
    {
        var response = new AuctionsListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        
        response.Auctions.AddRange(auctions.Select(MapToAuctionResponse));
        
        return response;
    }

    private static BidInfoMessage MapToBidInfoMessage(BidInfo bidInfo)
    {
        return new BidInfoMessage
        {
            BidderId = bidInfo.UserId,
            Amount = new MoneyMessage 
            { 
                Amount = (double)bidInfo.Amount.Amount, 
                Currency = bidInfo.Amount.Currency 
            },
            TimestampTicks = bidInfo.Timestamp.Ticks
        };
    }
    
    #endregion

    #region Mock Data (for demonstration)
    
    private Auction CreateMockAuction(string id)
    {
        var auction = Auction.Create(
            artworkName: $"Art Piece {id}",
            sellerId: "seller-123",
            startPrice: 1000m,
            startTime: DateTime.UtcNow.AddDays(-1),
            endTime: DateTime.UtcNow.AddDays(7),
            description: "Beautiful artwork"
        );
        
        // Start the auction
        auction.Start();
        
        // Place some mock bids
        auction.PlaceBid("bidder-456", Money.Create(1500m));
        
        return auction;
    }

    private List<Auction> CreateMockAuctionList(int pageNumber, int pageSize)
    {
        return Enumerable.Range(1, pageSize)
            .Select(i => CreateMockAuction($"auction-{pageNumber}-{i}"))
            .ToList();
    }
    
    #endregion

    #region Cache Invalidation
    
    private void InvalidateListCaches()
    {
        // Remove all list caches (this is simplified - in production use cache key patterns)
        _logger.LogInformation("Invalidating auction list caches");
        // In a real scenario, you'd track cache keys or use Redis key pattern scanning
    }
    
    #endregion
}
