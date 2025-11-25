namespace ArtAuction.Aggregator.Models;

/// <summary>
/// Aggregated auction data combining multiple service responses
/// </summary>
public class AggregatedAuctionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EndTime { get; set; }
    
    // Aggregated data from multiple sources
    public int TotalBids { get; set; }
    public SellerInfoDto? SellerInfo { get; set; }
    public HighestBidderInfoDto? HighestBidder { get; set; }
    
    // Metadata
    public DateTime AggregatedAt { get; set; }
    public List<string> SourceServices { get; set; } = new();
}

public class SellerInfoDto
{
    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public double SellerRating { get; set; }
}

public class HighestBidderInfoDto
{
    public string BidderId { get; set; } = string.Empty;
    public string BidderName { get; set; } = string.Empty;
    public decimal BidAmount { get; set; }
    public DateTime BidTime { get; set; }
}

public class AggregatedAuctionsListDto
{
    public List<AggregatedAuctionDto> Auctions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public DateTime AggregatedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
