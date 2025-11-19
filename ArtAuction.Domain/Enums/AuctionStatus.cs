namespace ArtAuction.Domain.Enums;

public enum AuctionStatus
{
    Pending = 0,
    Active = 1,
    Finished = 2,
    Cancelled = 3,
    Paid = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}
