namespace AuctionService.Domain.Entities;

/// <summary>
/// Аукціон
/// </summary>
public class Auction
{
    public long AuctionId { get; set; }
    public long ArtworkId { get; set; }
    public string ArtworkName { get; set; } = string.Empty;
    public long SellerUserId { get; set; }
    public decimal StartPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; }
    public long? WinnerUserId { get; set; }
}
