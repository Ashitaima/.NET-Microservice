using ArtAuction.Domain.Common;
using ArtAuction.Domain.Enums;
using ArtAuction.Domain.Exceptions;
using ArtAuction.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace ArtAuction.Domain.Entities;

[BsonCollection("auctions")]
public class Auction : BaseEntity
{
    [BsonElement("artwork_name")]
    public string ArtworkName { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("seller_id")]
    public string SellerId { get; private set; } = string.Empty;

    [BsonElement("start_price")]
    public Money StartPrice { get; private set; } = null!;

    [BsonElement("current_price")]
    public Money CurrentPrice { get; private set; } = null!;

    [BsonElement("start_time")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartTime { get; private set; }

    [BsonElement("end_time")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndTime { get; private set; }

    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public AuctionStatus Status { get; private set; }

    [BsonElement("winner_id")]
    public string? WinnerId { get; private set; }

    [BsonElement("bids")]
    public List<BidInfo> Bids { get; private set; } = new();

    private Auction() { } // For MongoDB deserialization

    private Auction(string artworkName, string sellerId, Money startPrice, 
                   DateTime startTime, DateTime endTime, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(artworkName))
            throw new DomainException("Artwork name cannot be empty");

        if (string.IsNullOrWhiteSpace(sellerId))
            throw new DomainException("Seller ID cannot be empty");

        if (startPrice.Amount <= 0)
            throw new DomainException("Start price must be greater than zero");

        if (endTime <= startTime)
            throw new DomainException("End time must be after start time");

        ArtworkName = artworkName;
        SellerId = sellerId;
        StartPrice = startPrice;
        CurrentPrice = startPrice;
        StartTime = startTime;
        EndTime = endTime;
        Description = description;
        Status = AuctionStatus.Pending;
    }

    public static Auction Create(string artworkName, string sellerId, decimal startPrice,
                                 DateTime startTime, DateTime endTime, string? description = null)
    {
        var price = Money.Create(startPrice);
        return new Auction(artworkName, sellerId, price, startTime, endTime, description);
    }

    public void PlaceBid(string userId, Money bidAmount)
    {
        if (Status != AuctionStatus.Active)
            throw new DomainException("Auction is not active");

        if (DateTime.UtcNow > EndTime)
            throw new DomainException("Auction has ended");

        if (userId == SellerId)
            throw new DomainException("Seller cannot bid on their own auction");

        if (!bidAmount.IsGreaterThan(CurrentPrice))
            throw new DomainException($"Bid amount must be greater than current price {CurrentPrice}");

        CurrentPrice = bidAmount;
        Bids.Add(new BidInfo(userId, bidAmount, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void Start()
    {
        if (Status != AuctionStatus.Pending)
            throw new DomainException("Only pending auctions can be started");

        if (DateTime.UtcNow < StartTime)
            throw new DomainException("Cannot start auction before start time");

        Status = AuctionStatus.Active;
        UpdateTimestamp();
    }

    public void Finish()
    {
        if (Status != AuctionStatus.Active)
            throw new DomainException("Only active auctions can be finished");

        Status = AuctionStatus.Finished;
        
        if (Bids.Count > 0)
        {
            var winningBid = Bids.OrderByDescending(b => b.Amount.Amount).First();
            WinnerId = winningBid.UserId;
        }

        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == AuctionStatus.Finished || Status == AuctionStatus.Paid)
            throw new DomainException("Cannot cancel finished or paid auctions");

        Status = AuctionStatus.Cancelled;
        UpdateTimestamp();
    }

    public void MarkAsPaid()
    {
        if (Status != AuctionStatus.Finished)
            throw new DomainException("Only finished auctions can be marked as paid");

        if (WinnerId == null)
            throw new DomainException("No winner to pay");

        Status = AuctionStatus.Paid;
        UpdateTimestamp();
    }
}

[BsonNoId]
public class BidInfo : ValueObject
{
    [BsonElement("user_id")]
    public string UserId { get; private set; } = string.Empty;

    [BsonElement("amount")]
    public Money Amount { get; private set; } = null!;

    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; private set; }

    private BidInfo() { } // For MongoDB deserialization

    public BidInfo(string userId, Money amount, DateTime timestamp)
    {
        UserId = userId;
        Amount = amount;
        Timestamp = timestamp;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Amount;
        yield return Timestamp;
    }
}
