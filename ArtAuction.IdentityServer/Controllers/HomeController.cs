using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Duende.IdentityServer.Services;

namespace ArtAuction.IdentityServer.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;

    public HomeController(IIdentityServerInteractionService interaction)
    {
        _interaction = interaction;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Error(string errorId)
    {
        var message = await _interaction.GetErrorContextAsync(errorId);
        return View(message);
    }
}
