using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ArtAuction.IdentityServer.Data;

namespace ArtAuction.IdentityServer.Services;

/// <summary>
/// Custom profile service to include role claims in tokens
/// </summary>
public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(UserManager<ApplicationUser> userManager, ILogger<ProfileService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var user = await _userManager.GetUserAsync(context.Subject);
        
        if (user == null)
        {
            var subjectId = context.Subject.FindFirst("sub")?.Value ?? "unknown";
            _logger.LogWarning("User not found for subject: {Subject}", subjectId);
            return;
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        
        // Add role claims
        var roleClaims = roles.Select(role => new Claim("role", role));
        context.IssuedClaims.AddRange(roleClaims);

        // Add other requested claims
        var claims = await _userManager.GetClaimsAsync(user);
        context.IssuedClaims.AddRange(claims);

        // Add standard claims
        context.IssuedClaims.Add(new Claim("sub", user.Id));
        context.IssuedClaims.Add(new Claim("email", user.Email ?? string.Empty));
        context.IssuedClaims.Add(new Claim("name", user.UserName ?? string.Empty));

        _logger.LogInformation("Profile data retrieved for user {UserId} with roles: {Roles}", 
            user.Id, string.Join(", ", roles));
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var user = await _userManager.GetUserAsync(context.Subject);
        context.IsActive = user != null;
    }
}
