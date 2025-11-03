namespace AuctionService.Bll.DTOs;

public class PaymentDto
{
    public long PaymentId { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentTime { get; set; }
    public int TransactionStatus { get; set; }
}
