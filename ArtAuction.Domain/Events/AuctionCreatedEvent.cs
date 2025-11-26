namespace ArtAuction.Domain.Events;

public record AuctionCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public string AuctionId { get; init; } = string.Empty;
    public string ArtworkName { get; init; } = string.Empty;
    public string SellerId { get; init; } = string.Empty;
    public decimal StartPriceAmount { get; init; }
    public string StartPriceCurrency { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
