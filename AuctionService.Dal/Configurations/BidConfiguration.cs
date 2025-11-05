using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Dal.Configurations;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bids");
        
        builder.HasKey(b => b.BidId);
        
        builder.Property(b => b.BidAmount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(b => b.Timestamp)
            .IsRequired();
        
        // Relationships
        builder.HasOne(b => b.Auction)
            .WithMany(a => a.Bids)
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(b => b.User)
            .WithMany(u => u.Bids)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(b => b.AuctionId);
        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.Timestamp);
    }
}
