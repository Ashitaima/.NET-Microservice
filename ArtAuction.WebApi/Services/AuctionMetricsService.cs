using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArtAuction.WebApi.Services;

/// <summary>
/// Lab #7: Custom business metrics for auction operations using OpenTelemetry Meter API
/// </summary>
public class AuctionMetricsService
{
    private readonly Meter _meter;
    private readonly Counter<long> _auctionsCreatedCounter;
    private readonly Counter<long> _bidsPlacedCounter;
    private readonly Histogram<double> _auctionOperationDuration;
    private readonly Histogram<double> _cacheOperationDuration;
    private readonly ObservableGauge<int> _activeCacheEntriesGauge;

    private int _activeCacheEntries;

    public AuctionMetricsService(IMeterFactory meterFactory)
    {
        // Create meter with service name
        _meter = meterFactory.Create("ArtAuction.WebApi", "1.0.0");

        // Counter: Total auctions created
        _auctionsCreatedCounter = _meter.CreateCounter<long>(
            name: "artauction.auctions.created",
            unit: "{auctions}",
            description: "Total number of auctions created");

        // Counter: Total bids placed
        _bidsPlacedCounter = _meter.CreateCounter<long>(
            name: "artauction.bids.placed",
            unit: "{bids}",
            description: "Total number of bids placed on auctions");

        // Histogram: Auction operation latency
        _auctionOperationDuration = _meter.CreateHistogram<double>(
            name: "artauction.operation.duration",
            unit: "ms",
            description: "Duration of auction operations in milliseconds");

        // Histogram: Cache operation latency
        _cacheOperationDuration = _meter.CreateHistogram<double>(
            name: "artauction.cache.duration",
            unit: "ms",
            description: "Duration of cache operations in milliseconds");

        // Gauge: Active cache entries (real-time state)
        _activeCacheEntriesGauge = _meter.CreateObservableGauge<int>(
            name: "artauction.cache.entries",
            observeValue: () => _activeCacheEntries,
            unit: "{entries}",
            description: "Current number of active cache entries");
    }

    /// <summary>
    /// Record auction creation event
    /// </summary>
    public void RecordAuctionCreated(string status, string sellerId)
    {
        _auctionsCreatedCounter.Add(1, new TagList
        {
            { "auction.status", status },
            { "seller.id", sellerId }
        });
    }

    /// <summary>
    /// Record bid placement event
    /// </summary>
    public void RecordBidPlaced(string auctionId, string bidderId, bool success)
    {
        _bidsPlacedCounter.Add(1, new TagList
        {
            { "auction.id", auctionId },
            { "bidder.id", bidderId },
            { "bid.success", success }
        });
    }

    /// <summary>
    /// Record auction operation duration
    /// </summary>
    public void RecordOperationDuration(string operation, double durationMs, string status, bool cacheHit = false)
    {
        _auctionOperationDuration.Record(durationMs, new TagList
        {
            { "operation", operation },
            { "status", status },
            { "cache.hit", cacheHit }
        });
    }

    /// <summary>
    /// Record cache operation duration
    /// </summary>
    public void RecordCacheDuration(string operation, double durationMs, string level, bool success)
    {
        _cacheOperationDuration.Record(durationMs, new TagList
        {
            { "cache.operation", operation }, // get, set, remove
            { "cache.level", level },         // L1 (memory), L2 (redis)
            { "cache.success", success }
        });
    }

    /// <summary>
    /// Update active cache entries count
    /// </summary>
    public void SetActiveCacheEntries(int count)
    {
        _activeCacheEntries = count;
    }

    /// <summary>
    /// Increment active cache entries
    /// </summary>
    public void IncrementCacheEntries()
    {
        Interlocked.Increment(ref _activeCacheEntries);
    }

    /// <summary>
    /// Decrement active cache entries
    /// </summary>
    public void DecrementCacheEntries()
    {
        Interlocked.Decrement(ref _activeCacheEntries);
    }
}
