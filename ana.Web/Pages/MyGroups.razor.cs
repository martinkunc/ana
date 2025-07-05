using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ana.Web.Layout;
using ana.SharedNet;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace ana.Web.Pages;

public partial class MyGroups : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    private UserSelectedGroupService userSelectedGroupService { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; }
    
    public List<AnaGroup> MyGroupsList { get; set; }

    private AnaGroup? selectedGroup { get; set; }

    private string displayedUserId { get; set; }

    private string? addGroupStatusMessage { get; set; }
    public string? GroupsLoadingStatus { get; set; }
    protected NewGroup newGroup { get; set; } = new NewGroup();
    protected EditContext editContext { get; set; }
    public string AddGroupSummary { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        editContext = new EditContext(newGroup);
        GroupsLoadingStatus = "Loading groups...";

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Console.WriteLine($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");
        displayedUserId = userId;

        var groups = await apiClient.GetGroupsAsync(userId);
        var selectedGroupRes = await apiClient.GetSelectedGroupAsync(userId);
        selectedGroup = selectedGroupRes?.AnaGroup;
        if (selectedGroup == null)
        {
            throw new InvalidOperationException("Couldn't find selected group for user.");
        }
        newGroup= new NewGroup { UserId = userId };
        editContext = new EditContext(newGroup);
        
        await RefreshGroupsAsync(userId);
    }

    protected async Task AddGroup()
    {
        if (!editContext.Validate())
        {
            AddGroupSummary = "Please correct the errors above.";
            return;
        }

        try
        {
            await apiClient.CreateGroupAsync(newGroup.UserId, newGroup.Name);
            addGroupStatusMessage = "Group was successfully added.";
        }
        catch (Exception e)
        {
            addGroupStatusMessage = "Adding of group was unsuccessful!";
            Console.WriteLine($"Adding of group was unsuccessful: {e.Message}");
        }

        Console.WriteLine($"Added group: {newGroup.Name}");

        await RefreshGroupsAsync(newGroup.UserId);
    }

    private async Task RefreshGroupsAsync(string userId)
    {
        newGroup = new NewGroup{ UserId = userId };
        editContext = new EditContext(newGroup);
        GroupsLoadingStatus = "Loading groups...";
        Console.WriteLine($"Refreshing groups for user {userId}");
        MyGroupsList = await apiClient.GetGroupsAsync(userId);
        if (!MyGroupsList.Any())
        {
            GroupsLoadingStatus = "No groups exists yet for this user.";
        }
        else
        {
            GroupsLoadingStatus = null;
        }
    }

    private async Task SwitchGroup(string groupId, string userId)
    {
        Console.WriteLine($"Switching group to {groupId} for userId {userId}");
        await apiClient.SelectGroupAsync(userId, groupId);
        Console.WriteLine("Raising selected group changed");
        userSelectedGroupService.RaiseChange();
        var selectedGroupRes = await apiClient.GetSelectedGroupAsync(userId);
        selectedGroup = selectedGroupRes?.AnaGroup;
        if (selectedGroup == null)
        {
            throw new InvalidOperationException("Couldn't find selected group for user.");
        }
        newGroup= new NewGroup { UserId = userId };
        editContext = new EditContext(newGroup);
        await RefreshGroupsAsync(userId);
        StateHasChanged();
    }



}