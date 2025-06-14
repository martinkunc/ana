using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


public class LogoutModel : PageModel
{
    private readonly IIdentityServerInteractionService _interactionService;
    private readonly SignInManager<IdentityUser> _signInManager;

    public LogoutModel(
        IIdentityServerInteractionService interactionService,
        SignInManager<IdentityUser> signInManager)
    {
        _interactionService = interactionService;
        _signInManager = signInManager;
    }

    [BindProperty(SupportsGet = true)]
    public string LogoutId { get; set; }

    public async Task<IActionResult> OnGet()
    {
        // Sign out of identity
        await _signInManager.SignOutAsync();

        // Get logout context
        var logout = await _interactionService.GetLogoutContextAsync(LogoutId);

        // Check if we need to trigger sign-out at an upstream identity provider
        if (logout?.PostLogoutRedirectUri != null)
        {
            // Redirect to the specified post logout URI
            return Redirect(logout.PostLogoutRedirectUri);
        }

        // If no redirect specified, redirect to home page
        return RedirectToPage("/");
    }
}