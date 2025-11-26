using ArtAuction.Auth.Service.Data;
using ArtAuction.Auth.Service.Models;
using ArtAuction.Auth.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtAuction.Auth.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false // У production має бути false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Assign default "User" role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate email verification token
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        _logger.LogInformation("User {Email} registered successfully. Verification token: {Token}", 
            user.Email, emailToken);

        // У production відправити email з посиланням
        // await _emailService.SendVerificationEmail(user.Email, emailToken);

        return Ok(new
        {
            message = "User registered successfully. Please verify your email.",
            userId = user.Id,
            verificationToken = emailToken // У production не повертати, відправити email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        if (!user.EmailConfirmed)
        {
            return Unauthorized(new { message = "Email not verified. Please verify your email first." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Unauthorized(new { message = "Account locked due to multiple failed login attempts." });
            }
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(claims);
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        // Save refresh token to database
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Set refresh token in HttpOnly cookie
        SetRefreshTokenCookie(refreshTokenString, refreshToken.ExpiresAt);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Get refresh token from cookie
        var refreshTokenFromCookie = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshTokenFromCookie))
        {
            return Unauthorized(new { message = "Refresh token not found" });
        }

        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenFromCookie);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        var user = refreshToken.User;

        // Generate new access token
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(claims);

        // Token rotation: revoke old refresh token та create new
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();
        refreshToken.ReplacedByToken = newRefreshTokenString;

        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenString,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        SetRefreshTokenCookie(newRefreshTokenString, newRefreshToken.ExpiresAt);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return Ok(new { accessToken = newAccessToken });
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken()
    {
        var refreshTokenFromCookie = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshTokenFromCookie))
        {
            return BadRequest(new { message = "Refresh token not found" });
        }

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenFromCookie);

        if (refreshToken == null)
        {
            return NotFound(new { message = "Refresh token not found" });
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked");

        return Ok(new { message = "Token revoked successfully" });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshTokenFromCookie = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshTokenFromCookie))
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenFromCookie);

            if (refreshToken != null)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _context.SaveChangesAsync();
            }
        }

        // Remove cookie
        Response.Cookies.Delete("refreshToken");

        _logger.LogInformation("User logged out");

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Email verified for user {Email}", user.Email);

        return Ok(new { message = "Email verified successfully" });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal if user exists
            return Ok(new { message = "If the email exists, a verification email has been sent." });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { message = "Email already verified" });
        }

        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        _logger.LogInformation("Verification email resent to {Email}. Token: {Token}", user.Email, emailToken);

        // У production відправити email
        // await _emailService.SendVerificationEmail(user.Email, emailToken);

        return Ok(new 
        { 
            message = "Verification email sent",
            verificationToken = emailToken // У production не повертати
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.EmailConfirmed)
        {
            // Don't reveal if user exists
            return Ok(new { message = "If the email exists, a password reset email has been sent." });
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        _logger.LogInformation("Password reset requested for {Email}. Token: {Token}", user.Email, resetToken);

        // У production відправити email
        // await _emailService.SendPasswordResetEmail(user.Email, resetToken);

        return Ok(new 
        { 
            message = "Password reset email sent",
            resetToken = resetToken // У production не повертати
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid request" });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Revoke всі refresh tokens для security
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = "password-reset";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for {Email}", user.Email);

        return Ok(new { message = "Password reset successfully" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Password changed for user {Email}", user.Email);

        return Ok(new { message = "Password changed successfully" });
    }

    private void SetRefreshTokenCookie(string token, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Security: не доступний з JavaScript
            Expires = expires,
            Secure = true, // HTTPS only
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };

        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
