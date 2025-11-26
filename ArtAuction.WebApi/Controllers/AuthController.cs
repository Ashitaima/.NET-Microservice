using ArtAuction.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtAuction.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IJwtTokenGenerator tokenGenerator, ILogger<AuthController> logger)
    {
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Login endpoint - returns JWT token (DEMO - without real user validation)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // DEMO: Replace with real user validation from database
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        // DEMO: Simulating user validation
        // In production: validate against database, check password hash
        var userId = Guid.NewGuid().ToString();
        var roles = request.Email.Contains("admin") 
            ? new List<string> { "Admin", "User" } 
            : new List<string> { "User" };

        var token = _tokenGenerator.GenerateToken(userId, request.Email, roles);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresIn = 3600,
            Email = request.Email,
            Roles = roles
        });
    }

    /// <summary>
    /// Test endpoint to verify authentication
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            userId,
            email,
            roles,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}
