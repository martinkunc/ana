using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IApiClient _apiClient;
    private readonly UserManager<IdentityUser> _userManager;

    private readonly ILogger<TokenService> _logger;

    public LoginModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ITokenService tokenService,
        IApiClient apiClient,
        ILogger<TokenService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _apiClient = apiClient;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public string ErrorMessage { get; set; }

    public class InputModel
    {
        public string? Email { get; set; }

        public string? Password { get; set; }

        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [Display(Name = "Group Name")]
        public string GroupName { get; set; }
        
        public string? IsRegistration { get; set; }
    }

    [BindProperty]
    public bool ShowRegistrationFields { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        Console.WriteLine($"OnGet called with returnUrl: {returnUrl}");

        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostRegisterAsync(string? returnUrl = null)
    {
        if (Input?.IsRegistration != "1")
        {
            ShowRegistrationFields = true;
            return Page();
        }

        if (ModelState.IsValid)
        {
            return await RegisterUser(returnUrl ?? "/");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostBackToLoginAsync(string? returnUrl = null)
    {
        return LocalRedirect(Url.Content("~/Account/Login"));
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ModelState.Remove("Input.Password");
        ModelState.Remove("Input.GroupName");
        ModelState.Remove("Input.DisplayName");
        Console.WriteLine($"OnPostAsync called with returnUrl: {returnUrl}");
        Console.WriteLine($"OnPostAsync called with IsRegistration: {Input.IsRegistration}");

        ReturnUrl = returnUrl ?? Url.Content("~/");
        if (ModelState.IsValid)
        {
            return await LoginUser(returnUrl);
        }
        return Page();
    }

    private async Task<IActionResult> RegisterUser(string? returnUrl = null)
    {
        if (Input == null || string.IsNullOrEmpty(Input.Email) || string.IsNullOrEmpty(Input.Password) || string.IsNullOrEmpty(Input.DisplayName) || string.IsNullOrEmpty(Input.GroupName))
        {
            ErrorMessage = "All fields are required.";
            return Page();
        }
        
        var user = new IdentityUser { UserName = Input.DisplayName, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input?.Password  ?? "");
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            if (User.Identity == null)
            {
                throw new InvalidOperationException("User identity is null after sign-in.");
            }


            _logger.LogInformation("Generated JWT token for user {Email} with Identity {Identity}", user.Email, User.Identity);
            
            await _apiClient.CreateGroupAsync(User.Identity, Input.GroupName);
            _logger.LogInformation("Group {GroupName} created for user {Email}", Input.GroupName, user.Email);


            return LocalRedirect(returnUrl ?? "/");
        }
        ErrorMessage = "Registration failed. Please try again.";
        foreach (var error in result.Errors)
        {
            ErrorMessage += error.Description;
        }
        return Page();
    }
    

    private async Task<IActionResult> LoginUser(string? returnUrl = null)
    {
        var result = await _signInManager.PasswordSignInAsync(Input.Email ?? "", Input.Password ?? "", false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }
        ErrorMessage = "Invalid login attempt.";
        if (result.IsLockedOut)
        {
            ErrorMessage = "User account locked out.";
        }
        else if (result.IsNotAllowed)
        {
            ErrorMessage = "User account not allowed to sign in.";
        }
        else
        {
            ErrorMessage = "Invalid login attempt.";
        }
        return Page();
    }
}