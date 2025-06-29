using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ana.Web.Layout;

namespace ana.Web.Pages;

public partial class Home : LayoutComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject]
    private IApiClient apiClient { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; }
    
    public List<AnaAnniv> Anniversaries { get; set; }
    public string AnniversariesLoadingStatus { get; set; }
    protected NewAnniversary newAnniversary { get; set; } = new NewAnniversary();
    protected EditContext editContext { get; set; }
    public string AddAnniversarySummary { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        editContext = new EditContext(newAnniversary);
        AnniversariesLoadingStatus = "Loading anniversaries...";

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Console.WriteLine($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        var group = await apiClient.GetSelectedGroupAsync(userId);
        Console.WriteLine($"Selected group: {group}");
        var groupId = group?.Id ?? throw new InvalidOperationException("Group ID not found in claims.");
        newAnniversary.GroupId = groupId;
        editContext = new EditContext(newAnniversary);


        Anniversaries = await apiClient.GetAnniversariesAsync(groupId);
        if (!Anniversaries.Any())
        {
            AnniversariesLoadingStatus = "No anniversaries exists yet in your group.";
        }
        else
        {
            AnniversariesLoadingStatus = null;
        }
    }

    protected async Task AddAnniversary()
    {
        if (!editContext.Validate())
        {
            AddAnniversarySummary = "Please correct the errors above.";
            return;
        }
    
        if (string.IsNullOrEmpty(newAnniversary.Id))
        {
             
            var na = new AnaAnniv { Date = FormatDate(newAnniversary.Date), Name = newAnniversary.Name, GroupId = newAnniversary.GroupId };
            await apiClient.CreateAnniversaryAsync(newAnniversary.GroupId, na);
        }
        else
        {
            // Update existing
            var na = new AnaAnniv { Id=newAnniversary.Id, Date = FormatDate(newAnniversary.Date), Name = newAnniversary.Name, GroupId = newAnniversary.GroupId };
            await apiClient.UpdateAnniversaryAsync(na);
        }

        Console.WriteLine($"Adding new anniversary: {newAnniversary.Name} on {newAnniversary.Date}");

        //Anniversaries.Add(new Anniversary { Date = newAnniversary.Date, Name = newAnniversary.Name });

        await RefreshAnniversaries(newAnniversary.GroupId);

    }

    private async Task RefreshAnniversaries(string groupId)
    {
        // await apiClient.CreateAnniversaryAsync(group.Id, na);
        newAnniversary = new NewAnniversary{ GroupId = groupId };
        editContext = new EditContext(newAnniversary);

        AnniversariesLoadingStatus = "Loading anniversaries...";
        Anniversaries = await apiClient.GetAnniversariesAsync(groupId);
        if (!Anniversaries.Any())
        {
            AnniversariesLoadingStatus = "No anniversaries exists yet in your group.";
        }
        else
        {
            AnniversariesLoadingStatus = null;
        }
    }

    private async Task EditAnniversary(string id, string groupId, string date, string name)
    {
        Console.WriteLine($"Editing anniversary: {name} on {date}");

        newAnniversary = new NewAnniversary
        {
            Id = id,
            GroupId = groupId,
            Name = name,
            Date = date
        };
        editContext = new EditContext(newAnniversary);
        StateHasChanged();
    }

    private async Task RemoveAnniversary(string anniversaryId, string groupId)
    {
        bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to remove this anniversary?");
        if (!confirmed)
            return;

        // Remove from backend if needed
        await apiClient.DeleteAnniversaryAsync(anniversaryId, groupId);
        // Remove from local list
        Anniversaries = Anniversaries.Where(a => a.Id != anniversaryId).ToList();
        StateHasChanged();
        await RefreshAnniversaries(groupId);
    }

    private string FormatDate(string inputDate)
    {
        var date = DateOnly.ParseExact(inputDate, "d/M", null);
        return date.Day + "/" + date.Month;
    }
}