using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionService.Dal.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.UserId);
        
        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.Balance)
            .HasPrecision(18, 2);
        
        // Index for unique username
        builder.HasIndex(u => u.UserName).IsUnique();
    }
}
