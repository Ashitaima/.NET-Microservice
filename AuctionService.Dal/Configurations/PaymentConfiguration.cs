using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Dal.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.PaymentId);
        
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(p => p.PaymentTime)
            .IsRequired();
        
        builder.Property(p => p.TransactionStatus)
            .IsRequired();
        
        // Relationships
        builder.HasOne(p => p.Auction)
            .WithOne(a => a.Payment)
            .HasForeignKey<Payment>(p => p.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(p => p.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(p => p.AuctionId).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.TransactionStatus);
    }
}
