namespace AuctionService.Domain.Entities;

/// <summary>
/// Платіж за аукціон (1:1 з Auction)
/// </summary>
public class Payment
{
    public long PaymentId { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentTime { get; set; }
    public TransactionStatus TransactionStatus { get; set; }
}
