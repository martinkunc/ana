using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogoutModel : PageModel
{
    private readonly IIdentityServerInteractionService _interactionService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        IIdentityServerInteractionService interactionService,
        SignInManager<IdentityUser> signInManager,
        ILogger<LogoutModel> logger)
    {
        _interactionService = interactionService;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string LogoutId { get; set; }

    public async Task<IActionResult> OnGet()
    {
        _logger.LogInformation("Logout GET request with LogoutId: {LogoutId}", LogoutId);

        var vm = await BuildLogoutViewModelAsync(LogoutId);

        if (vm.ShowLogoutPrompt == false)
        {
            return await OnPost();
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var logout = await _interactionService.GetLogoutContextAsync(LogoutId);

        if (User?.Identity.IsAuthenticated == true)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserId} logged out", User.Identity.Name);
        }

        var postLogoutUri = logout?.PostLogoutRedirectUri;

        if (!string.IsNullOrEmpty(postLogoutUri))
        {
            _logger.LogInformation("Redirecting to post logout URI: {Uri}", postLogoutUri);
            if (Url.IsLocalUrl(postLogoutUri))
            {
                return LocalRedirect(postLogoutUri);
            }
        
            Response.Redirect(postLogoutUri);
            return new EmptyResult();
        }

        _logger.LogInformation("No post logout URI found");
        return Redirect("/Account/Login");
    }

    private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
    {
        var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

        if (User?.Identity.IsAuthenticated != true)
        {
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        var context = await _interactionService.GetLogoutContextAsync(logoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        return vm;
    }
}

public class LogoutViewModel
{
    public string LogoutId { get; set; }
    public bool ShowLogoutPrompt { get; set; } = true;
}

public static class AccountOptions
{
    public static bool ShowLogoutPrompt = true;
    public static bool AutomaticRedirectAfterSignOut = false;
}
