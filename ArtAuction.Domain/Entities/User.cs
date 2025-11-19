using ArtAuction.Domain.Common;
using ArtAuction.Domain.Exceptions;
using ArtAuction.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace ArtAuction.Domain.Entities;

[BsonCollection("users")]
public class User : BaseEntity
{
    [BsonElement("user_name")]
    public string UserName { get; private set; } = string.Empty;

    [BsonElement("email")]
    public Email Email { get; private set; } = null!;

    [BsonElement("balance")]
    public Money Balance { get; private set; } = null!;

    [BsonElement("address")]
    public Address? Address { get; private set; }

    [BsonElement("is_active")]
    public bool IsActive { get; private set; }

    private User() { } // For MongoDB deserialization

    private User(string userName, Email email, Money balance, Address? address = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("User name cannot be empty");

        UserName = userName;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Balance = balance ?? throw new ArgumentNullException(nameof(balance));
        Address = address;
        IsActive = true;
    }

    public static User Create(string userName, string email, decimal initialBalance = 0, Address? address = null)
    {
        var emailValue = Email.Create(email);
        var balance = Money.Create(initialBalance);
        return new User(userName, emailValue, balance, address);
    }

    public void UpdateBalance(Money newBalance)
    {
        if (newBalance.Amount < 0)
            throw new DomainException("Balance cannot be negative");

        Balance = newBalance;
        UpdateTimestamp();
    }

    public void Deposit(Money amount)
    {
        if (amount.Amount <= 0)
            throw new DomainException("Deposit amount must be positive");

        Balance = Balance.Add(amount);
        UpdateTimestamp();
    }

    public void Withdraw(Money amount)
    {
        if (amount.Amount <= 0)
            throw new DomainException("Withdrawal amount must be positive");

        if (!Balance.IsGreaterThan(amount) && Balance.Amount != amount.Amount)
            throw new DomainException("Insufficient balance");

        Balance = Balance.Subtract(amount);
        UpdateTimestamp();
    }

    public void UpdateAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}
