using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtAuction.Auth.Service.Data;

/// <summary>
/// Refresh Token entity for token rotation mechanism
/// </summary>
public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }
    
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    [NotMapped]
    public bool IsRevoked => RevokedAt != null;
    
    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
    
    // Navigation property
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}
