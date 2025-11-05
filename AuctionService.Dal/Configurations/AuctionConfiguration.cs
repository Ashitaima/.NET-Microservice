using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Dal.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("Auctions");
        
        builder.HasKey(a => a.AuctionId);
        
        builder.Property(a => a.ArtworkName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(a => a.StartPrice)
            .HasPrecision(18, 2);
        
        builder.Property(a => a.CurrentPrice)
            .HasPrecision(18, 2);
        
        builder.Property(a => a.StartTime)
            .IsRequired();
        
        builder.Property(a => a.EndTime)
            .IsRequired();
        
        builder.Property(a => a.Status)
            .IsRequired();
        
        // Relationships
        builder.HasOne(a => a.Seller)
            .WithMany(u => u.SellerAuctions)
            .HasForeignKey(a => a.SellerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(a => a.Winner)
            .WithMany(u => u.WonAuctions)
            .HasForeignKey(a => a.WinnerUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        
        builder.HasMany(a => a.Bids)
            .WithOne(b => b.Auction)
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(a => a.Payment)
            .WithOne(p => p.Auction)
            .HasForeignKey<Payment>(p => p.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.SellerUserId);
        builder.HasIndex(a => a.EndTime);
    }
}
