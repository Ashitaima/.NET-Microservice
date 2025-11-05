using AuctionService.Dal.Configurations;
using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Dal;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
    {
    }

    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new AuctionConfiguration());
        modelBuilder.ApplyConfiguration(new BidConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());

        // Data seeding
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Users
        var users = new[]
        {
            new User { UserId = 1, UserName = "john_doe", Balance = 10000.00m },
            new User { UserId = 2, UserName = "jane_smith", Balance = 15000.00m },
            new User { UserId = 3, UserName = "bob_wilson", Balance = 8000.00m },
            new User { UserId = 4, UserName = "alice_brown", Balance = 20000.00m },
            new User { UserId = 5, UserName = "charlie_davis", Balance = 12000.00m }
        };
        modelBuilder.Entity<User>().HasData(users);

        // Seed Auctions
        var now = DateTime.UtcNow;
        var auctions = new[]
        {
            new Auction
            {
                AuctionId = 1,
                ArtworkId = 101,
                ArtworkName = "Starry Night Reproduction",
                SellerUserId = 1,
                StartPrice = 1000.00m,
                CurrentPrice = 1500.00m,
                StartTime = now.AddDays(-5),
                EndTime = now.AddDays(2),
                Status = AuctionStatus.Active,
                WinnerUserId = null
            },
            new Auction
            {
                AuctionId = 2,
                ArtworkId = 102,
                ArtworkName = "Mona Lisa Copy",
                SellerUserId = 2,
                StartPrice = 2000.00m,
                CurrentPrice = 3500.00m,
                StartTime = now.AddDays(-10),
                EndTime = now.AddDays(-1),
                Status = AuctionStatus.Finished,
                WinnerUserId = 4
            },
            new Auction
            {
                AuctionId = 3,
                ArtworkId = 103,
                ArtworkName = "Abstract Art #5",
                SellerUserId = 3,
                StartPrice = 500.00m,
                CurrentPrice = 500.00m,
                StartTime = now.AddDays(-2),
                EndTime = now.AddDays(5),
                Status = AuctionStatus.Active,
                WinnerUserId = null
            },
            new Auction
            {
                AuctionId = 4,
                ArtworkId = 104,
                ArtworkName = "Modern Sculpture",
                SellerUserId = 1,
                StartPrice = 3000.00m,
                CurrentPrice = 5000.00m,
                StartTime = now.AddDays(-7),
                EndTime = now.AddDays(-2),
                Status = AuctionStatus.Finished,
                WinnerUserId = 5
            },
            new Auction
            {
                AuctionId = 5,
                ArtworkId = 105,
                ArtworkName = "Landscape Painting",
                SellerUserId = 4,
                StartPrice = 800.00m,
                CurrentPrice = 800.00m,
                StartTime = now.AddDays(1),
                EndTime = now.AddDays(7),
                Status = AuctionStatus.Pending,
                WinnerUserId = null
            }
        };
        modelBuilder.Entity<Auction>().HasData(auctions);

        // Seed Bids
        var bids = new[]
        {
            new Bid { BidId = 1, AuctionId = 1, UserId = 2, BidAmount = 1100.00m, Timestamp = now.AddDays(-4) },
            new Bid { BidId = 2, AuctionId = 1, UserId = 3, BidAmount = 1300.00m, Timestamp = now.AddDays(-3) },
            new Bid { BidId = 3, AuctionId = 1, UserId = 4, BidAmount = 1500.00m, Timestamp = now.AddDays(-2) },
            new Bid { BidId = 4, AuctionId = 2, UserId = 3, BidAmount = 2500.00m, Timestamp = now.AddDays(-8) },
            new Bid { BidId = 5, AuctionId = 2, UserId = 4, BidAmount = 3500.00m, Timestamp = now.AddDays(-7) },
            new Bid { BidId = 6, AuctionId = 4, UserId = 2, BidAmount = 3500.00m, Timestamp = now.AddDays(-6) },
            new Bid { BidId = 7, AuctionId = 4, UserId = 5, BidAmount = 5000.00m, Timestamp = now.AddDays(-5) }
        };
        modelBuilder.Entity<Bid>().HasData(bids);

        // Seed Payments
        var payments = new[]
        {
            new Payment
            {
                PaymentId = 1,
                AuctionId = 2,
                UserId = 4,
                Amount = 3500.00m,
                PaymentTime = now.AddDays(-1).AddHours(2),
                TransactionStatus = TransactionStatus.Success
            },
            new Payment
            {
                PaymentId = 2,
                AuctionId = 4,
                UserId = 5,
                Amount = 5000.00m,
                PaymentTime = now.AddDays(-2).AddHours(3),
                TransactionStatus = TransactionStatus.Success
            }
        };
        modelBuilder.Entity<Payment>().HasData(payments);
    }
}
