using ArtAuction.Domain.Common;
using ArtAuction.Domain.Enums;
using ArtAuction.Domain.Exceptions;
using ArtAuction.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace ArtAuction.Domain.Entities;

[BsonCollection("payments")]
public class Payment : BaseEntity
{
    [BsonElement("auction_id")]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string AuctionId { get; private set; } = string.Empty;

    [BsonElement("user_id")]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string UserId { get; private set; } = string.Empty;

    [BsonElement("amount")]
    public Money Amount { get; private set; } = null!;

    [BsonElement("payment_time")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PaymentTime { get; private set; }

    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public PaymentStatus Status { get; private set; }

    [BsonElement("transaction_id")]
    public string? TransactionId { get; private set; }

    private Payment() { } // For MongoDB deserialization

    private Payment(string auctionId, string userId, Money amount)
    {
        if (string.IsNullOrWhiteSpace(auctionId))
            throw new DomainException("Auction ID cannot be empty");

        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty");

        if (amount.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero");

        AuctionId = auctionId;
        UserId = userId;
        Amount = amount;
        PaymentTime = DateTime.UtcNow;
        Status = PaymentStatus.Pending;
    }

    public static Payment Create(string auctionId, string userId, decimal amount)
    {
        var paymentAmount = Money.Create(amount);
        return new Payment(auctionId, userId, paymentAmount);
    }

    public void Complete(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only pending payments can be completed");

        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID cannot be empty", nameof(transactionId));

        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        UpdateTimestamp();
    }

    public void Fail()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Only pending payments can be failed");

        Status = PaymentStatus.Failed;
        UpdateTimestamp();
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Completed)
            throw new DomainException("Only completed payments can be refunded");

        Status = PaymentStatus.Refunded;
        UpdateTimestamp();
    }
}
