using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public string ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        public string Email { get; set; }
        
        public string Password { get; set; }
    }

    public void OnGet(string returnUrl = null)
    {
        Console.WriteLine($"OnGet called with returnUrl: {returnUrl}");

        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        ModelState.Remove("Input.Password");
        Console.WriteLine($"OnPostAsync called with returnUrl: {returnUrl}");

        ReturnUrl = returnUrl ?? Url.Content("~/");
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password ?? "", false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return LocalRedirect(ReturnUrl);
            }
            ErrorMessage = "Invalid login attempt.";
        }
        return Page();
    }
}