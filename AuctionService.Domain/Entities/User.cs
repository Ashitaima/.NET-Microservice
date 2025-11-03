namespace AuctionService.Domain.Entities;

/// <summary>
/// Користувач (дубльовані дані з User Service)
/// </summary>
public class User
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
