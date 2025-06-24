using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace ana.Web.Pages;

public partial class Settings : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;

    [Inject]
    private UserDisplayNameService DisplayNameService { get; set; } = default!;

    private string? saveStatusMessage;
    protected override async Task OnInitializedAsync()
    {
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
        // Add your cancel logic here
        await apiClient.UpdateUserSettingsAsync(settingsModel.Id, settingsModel);
        DisplayNameService.DisplayName = settingsModel.DisplayName;
        saveStatusMessage = "Settings saved successfully!";
        Console.WriteLine($"Settings saved: {settingsModel.DisplayName}, {settingsModel.WhatsAppNumber}, {settingsModel.PreferredNotification}");
    }

    private void CancelAccount()
    {
        // Add your cancel logic here
    }



}


