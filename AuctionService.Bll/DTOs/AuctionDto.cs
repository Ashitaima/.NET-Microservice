namespace AuctionService.Bll.DTOs;

public class AuctionDto
{
    public long AuctionId { get; set; }
    public long ArtworkId { get; set; }
    public string ArtworkName { get; set; } = string.Empty;
    public long SellerUserId { get; set; }
    public decimal StartPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Status { get; set; }
    public long? WinnerUserId { get; set; }
}

public class CreateAuctionDto
{
    public long ArtworkId { get; set; }
    public string ArtworkName { get; set; } = string.Empty;
    public long SellerUserId { get; set; }
    public decimal StartPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
