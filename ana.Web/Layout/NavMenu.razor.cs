using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ana.Web.Layout;

public partial class NavMenu : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;

    [Inject]
    private UserSelectedGroupService userSelectedGroupService { get; set; } = default!;

    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    public string? AnaGroupName { get; set; }
    private Action? GroupRefreshDelegate { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AnaGroupName = "Loading...";
        Console.WriteLine("Adding StateHasChanged OnChange listener");
        GroupRefreshDelegate = async () => await RefreshSelectedGroup();
        userSelectedGroupService.OnChange += GroupRefreshDelegate;

        await RefreshSelectedGroup();
    }

    public void Dispose()
    {
        Console.WriteLine("unsubscribing change");
        userSelectedGroupService.OnChange -= GroupRefreshDelegate;
    }

    private async Task RefreshSelectedGroup()
    {
        Console.WriteLine("Refreshing NavMenu selected group");
        if (AuthenticationStateProvider == null)
        {
            throw new InvalidOperationException("AuthenticationStateProvider is not initialized.");
        }
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Console.WriteLine($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        var selectedGroup = await apiClient.GetSelectedGroupAsync(userId);
        var group = selectedGroup?.AnaGroup;
        Console.WriteLine($"Selected group: {group}");
        if (group != null)
        {
            AnaGroupName = group.Name;
        }
        else
        {
            AnaGroupName = "No groups found";
        }
        StateHasChanged();
    }

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}