using System.Security.Claims;

namespace ArtAuction.Auth.Service.Services;

/// <summary>
/// Service для generation та validation JWT tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access token з claims
    /// </summary>
    string GenerateAccessToken(IEnumerable<Claim> claims);
    
    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validate token та отримати ClaimsPrincipal
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
