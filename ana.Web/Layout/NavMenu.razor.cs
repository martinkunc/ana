using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ana.Web.Layout;

public partial class NavMenu : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;
    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    public string? AnaGroupName { get; set; }


    protected override async Task OnInitializedAsync()
    {
        AnaGroupName = "Loading...";

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Console.WriteLine($"User is authenticated: {string.Join(",",authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        var userId = authState.User.Claims.FirstOrDefault( c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        var groups = await apiClient.GetGroupsAsync(userId);
        if (groups != null && groups.Any()) {
            AnaGroupName = groups.FirstOrDefault()?.Name;
        }
        else
        {
            AnaGroupName = "No groups found";
        }    
    }

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}