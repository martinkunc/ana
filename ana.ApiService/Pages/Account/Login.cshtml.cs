using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;
using System.Linq;
using ana.Web.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    
    private readonly IApiClient _apiClient;
    private readonly UserManager<IdentityUser> _userManager;

    private readonly ILogger<LoginModel> _logger;
    private readonly IWebHostEnvironment _environment;

    public LoginModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IApiClient apiClient,
        ILogger<LoginModel> logger,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _apiClient = apiClient;
        _logger = logger;
        _environment = environment;
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

        [Display(Name = "I consent to the use of essential cookies")]
        [Required(ErrorMessage = "You must consent to the use of essential cookies.")]
        public bool ConsentCookies { get; set; }

        [Display(Name = "I am at least 13 years old and agree to the data handling policy")]
        [Required(ErrorMessage = "You must confirm your age and agree to the data policy.")]
        public bool ConsentAgeAndPolicy { get; set; }
        
        public string? InvitationCode { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        Console.WriteLine($"OnGet called with returnUrl: {returnUrl}");
        Input = new InputModel();
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostRegisterAsync(string? returnUrl = null)
    {
        if (Input?.IsRegistration != "1")
        {
            if (Input != null)
            {
                Input.IsRegistration = "1";
            }
            return Page();
        }

        if (ModelState.IsValid)
        {
            if (!Input.ConsentCookies || !Input.ConsentAgeAndPolicy)
            {
                ErrorMessage = "For your registration you have to agree with use of Cookies and the Data handling policy. Please check your consents.";
                return Page();
            }

            if (!IsValidInvitationCode(Input.InvitationCode))
            {
                ErrorMessage = "Invalid invitation code";
                return Page();
            }

            return await RegisterUser(returnUrl ?? "/");
        }
        return Page();
    }

    private bool IsValidInvitationCode(string invitationCode)
    {
        if (_environment.IsDevelopment())
        {
            // In development, allow any code
            return true;
        }
        if (int.TryParse(invitationCode, out var code))
        {
            if (code == int.Parse(DateTime.Now.ToString("yyyyMMdd")) + 2)
                return true;
        }
        return false;
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
            var userId = User.FindFirst(JwtClaimTypes.Subject)?.Value ?? throw new InvalidOperationException("User ID not found in claims.");
            var createGroupResponse = await _apiClient.CreateGroupAsync(userId, Input.GroupName);
            _logger.LogInformation("Group {GroupName} created for user {Email}", Input.GroupName, user.Email);

            await _apiClient.CreateUserAsync(new AnaUser
            {
                Id = userId,
                DisplayName = Input.DisplayName,
                WhatsAppNumber = "",
                PreferredNotification = NotificationType.None.ToString(),
                SelectedGroupId = createGroupResponse.Group.Id
            });


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
        else
        {
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
}