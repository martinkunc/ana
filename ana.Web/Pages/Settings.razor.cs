using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;

namespace ana.Web.Pages;

public partial class Settings : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [Inject]
    private UserDisplayNameService DisplayNameService { get; set; } = default!;
    [Inject]
    private NavigationManager? Navigation { get; set; }

    private string? saveStatusMessage;
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateProvider == null)
        {
            throw new InvalidOperationException("AuthenticationStateProvider is not available.");
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Console.WriteLine($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        var sett = await apiClient.GetUserSettingsAsync(userId);
        Console.WriteLine($"User settings retrieved: {sett.DisplayName}, {sett.WhatsAppNumber}, {sett.PreferredNotification}");
        settingsModel = sett;
    }

    protected AnaUser settingsModel = new AnaUser();

    private async Task SaveSettings()
    {
        try
        {
            await apiClient.UpdateUserSettingsAsync(settingsModel.Id, settingsModel);
            DisplayNameService.DisplayName = settingsModel.DisplayName;
            saveStatusMessage = "Settings saved successfully!";
            Console.WriteLine($"Settings saved: {settingsModel.DisplayName}, {settingsModel.WhatsAppNumber}, {settingsModel.PreferredNotification}");
        }
        catch (Exception)
        {
            saveStatusMessage = "Settings weren't saved!";
            Console.WriteLine($"Settings saved: {settingsModel.DisplayName}, {settingsModel.WhatsAppNumber}, {settingsModel.PreferredNotification}");
        }
    }

    private async Task CancelAccount()
    {
        if (JSRuntime == null)
        {
            throw new InvalidOperationException("JSRuntime is not available.");
        }
        bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to cancel your account?");
        if (!confirmed)
            return;
        try
        {
            await apiClient.CancelUserAsync(settingsModel.Id);
            saveStatusMessage = "User cancelled successfully!";
            Navigation?.NavigateToLogout("authentication/logout");
            Console.WriteLine($"User cancelled: {settingsModel.Id}");
        }
        catch (Exception e)
        {
            saveStatusMessage = "User wasn't cancelled!";
            Console.WriteLine($"User wasn't cancelled: {e.Message}");
        }
    }
}