namespace ArtAuction.Domain.Events;

public record BidPlacedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public string AuctionId { get; init; } = string.Empty;
    public string BidderId { get; init; } = string.Empty;
    public decimal BidAmount { get; init; }
    public decimal PreviousPrice { get; init; }
    public int TotalBids { get; init; }
}
