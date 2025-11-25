using ArtAuction.Aggregator.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArtAuction.Aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AggregatedAuctionsController : ControllerBase
{
    private readonly AuctionAggregatorService _aggregatorService;
    private readonly ILogger<AggregatedAuctionsController> _logger;

    public AggregatedAuctionsController(
        AuctionAggregatorService aggregatorService,
        ILogger<AggregatedAuctionsController> logger)
    {
        _aggregatorService = aggregatorService;
        _logger = logger;
    }

    /// <summary>
    /// Get aggregated auction details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAggregatedAuction(string id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Aggregating auction data for {AuctionId}", id);
        
        var result = await _aggregatorService.GetAggregatedAuctionAsync(id, cancellationToken);
        
        if (result == null)
            return NotFound(new { Message = $"Auction {id} not found" });
        
        return Ok(result);
    }

    /// <summary>
    /// Get aggregated auctions list with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAggregatedAuctions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aggregating auctions list: page {Page}, size {Size}", pageNumber, pageSize);
        
        var result = await _aggregatorService.GetAggregatedAuctionsAsync(
            pageNumber, 
            pageSize, 
            cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Service = "Aggregator", Timestamp = DateTime.UtcNow });
    }
}
