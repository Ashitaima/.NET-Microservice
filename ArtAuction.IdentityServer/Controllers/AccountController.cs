using ArtAuction.IdentityServer;
using ArtAuction.IdentityServer.Data;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtAuction.IdentityServer.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityServerInteractionService interaction,
        IEventService events)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _interaction = interaction;
        _events = events;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl)
    {
        var vm = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Username, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            await _events.RaiseAsync(new UserLoginSuccessEvent(
                user!.UserName, 
                user.Id, 
                user.UserName));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect("~/");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account is locked out.");
        }
        else
        {
            await _events.RaiseAsync(new UserLoginFailureEvent(
                model.Username, 
                "invalid credentials"));
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout(string? logoutId)
    {
        var vm = new LogoutViewModel
        {
            LogoutId = logoutId
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            await _signInManager.SignOutAsync();
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        }

        var logout = await _interaction.GetLogoutContextAsync(logoutId);
        vm.PostLogoutRedirectUri = logout?.PostLogoutRedirectUri;

        return View(vm);
    }
}

public static class PrincipalExtensions
{
    public static string? GetSubjectId(this System.Security.Claims.ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value;
    }

    public static string? GetDisplayName(this System.Security.Claims.ClaimsPrincipal principal)
    {
        return principal.Identity?.Name;
    }
}
