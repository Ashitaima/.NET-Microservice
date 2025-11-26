namespace ArtAuction.Domain.Events;

public record AuctionFinishedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    public string AuctionId { get; init; } = string.Empty;
    public string? WinnerId { get; init; }
    public decimal? FinalPrice { get; init; }
    public int TotalBids { get; init; }
    public bool HasWinner { get; init; }
}
