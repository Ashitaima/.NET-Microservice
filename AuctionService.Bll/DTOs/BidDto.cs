namespace AuctionService.Bll.DTOs;

public class BidDto
{
    public long BidId { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal BidAmount { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PlaceBidDto
{
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal BidAmount { get; set; }
}
