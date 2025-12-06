namespace ArtAuction.IdentityServer;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class LogoutViewModel
{
    public string? LogoutId { get; set; }
    public string? PostLogoutRedirectUri { get; set; }
}
