namespace AuctionService.Bll.DTOs;

public class UserDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
