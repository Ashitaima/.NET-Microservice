namespace AuctionService.Domain.Entities;

/// <summary>
/// Ставка на аукціоні
/// </summary>
public class Bid
{
    public long BidId { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal BidAmount { get; set; }
    public DateTime Timestamp { get; set; }
}
