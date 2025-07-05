using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ana.Web.Layout;
using ana.SharedNet;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using Azure.Core.Pipeline;

namespace ana.Web.Pages;

public partial class Members : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string MembersOfGroupTitle { get; set; } = "Members";
    
    public List<AnaGroupMember> GroupMembers { get; set; }

    private bool isAdmin { get; set; } = false;

    private string displayedGroupId { get; set; } = string.Empty;

    private string? addMemberStatusMessage { get; set; }
    public string? MembersLoadingStatus { get; set; }
    protected NewGroupUser newUser { get; set; } = new NewGroupUser();
    protected EditContext editContext { get; set; }
    public string AddGroupUserSummary { get; set; } = string.Empty;
    public static class Colors
    {
        public static string Red = "red";
        public static string Green = "#4caf50";
    }
    private string addMemberStatusColor { get; set; }  = Colors.Green;
    protected override async Task OnInitializedAsync()
    {
        editContext = new EditContext(newUser);
        MembersLoadingStatus = "Loading members...";

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Console.WriteLine($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        var selectedGroup = await apiClient.GetSelectedGroupAsync(userId);
        var group = selectedGroup?.AnaGroup;
        if (group == null)
        {
            throw new InvalidOperationException("Coulnd't find selected group for user.");
        }
        isAdmin = selectedGroup?.UserRole == AnaRoleNames.Admin;
        displayedGroupId = group.Id;

        newUser = new NewGroupUser { GroupId = displayedGroupId };
        editContext = new EditContext(newUser);

        Console.WriteLine($"Selected group: {group}");
        var groupId = group?.Id ?? throw new InvalidOperationException("Group ID not found.");
        Console.WriteLine($"Group ID: {groupId}");
        MembersOfGroupTitle = $"Members of group {group.Name}";
        await RefreshGroupMembersAsync(newUser.GroupId);
    }

    protected async Task AddGroupMember()
    {
        if (!editContext.Validate())
        {
            AddGroupUserSummary = "Please correct the errors above.";
            return;
        }

        try
        {
            await apiClient.CreateGroupMemberAsync(newUser.GroupId, newUser.Email);
            addMemberStatusMessage = "User was successfully added.";
            addMemberStatusColor = Colors.Green;
        }
        catch (Exception e)
        {
            addMemberStatusMessage = "Failed to add user!";
            addMemberStatusColor = Colors.Red;
            Console.WriteLine($"Adding member was unsuccessful: {e.Message}");
        }

        Console.WriteLine($"Added member: {newUser.Email}");

        await RefreshGroupMembersAsync(newUser.GroupId);
    }

    private async Task RefreshGroupMembersAsync(string groupId)
    {
        newUser = new NewGroupUser{ GroupId = groupId };
        editContext = new EditContext(newUser);
        MembersLoadingStatus = "Loading members...";
        Console.WriteLine($"Refreshing group members for groupId {groupId}");
        GroupMembers = await apiClient.GetGroupMembersAsync(groupId);
        if (!GroupMembers.Any())
        {
            MembersLoadingStatus = "No members exists yet in this group.";
        }
        else
        {
            MembersLoadingStatus = null;
        }
    }

    private async Task CheckAdminChanged(object isAdmin, string userId, string groupId)
    {
        if (!bool.TryParse(isAdmin?.ToString(), out var isAdminBool)) {
            return;
        }
        Console.WriteLine($"Changed is admin for: {userId} in {groupId} to {isAdminBool}");
        var roleName = isAdminBool switch
        {
            true => AnaRoleNames.Admin,
            false => AnaRoleNames.User,
        };
        Console.WriteLine($"Changing role for group {groupId} userId {userId} to roleName {roleName}");
        await apiClient.ChangeGroupMemberRoleAsync(groupId, userId, roleName);
        
        
        StateHasChanged();
        await RefreshGroupMembersAsync(groupId);
        StateHasChanged();
    }



    private async Task RemoveMember(string groupId, string userId)
    {
        bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to remove the user from this group ?");
        if (!confirmed)
            return;

        // Remove from backend if needed
        await apiClient.DeleteGroupMemberAsync(groupId, userId);
        // Remove from local list
        GroupMembers = GroupMembers.Where(m => m.UserId != userId).ToList();
        StateHasChanged();
        await RefreshGroupMembersAsync(groupId);
    }
}