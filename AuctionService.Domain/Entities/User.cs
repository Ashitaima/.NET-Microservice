namespace AuctionService.Domain.Entities;

/// <summary>
/// Користувач (дубльовані дані з User Service)
/// </summary>
public class User
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    
    // Navigation properties
    public ICollection<Auction> SellerAuctions { get; set; } = new List<Auction>();
    public ICollection<Auction> WonAuctions { get; set; } = new List<Auction>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
