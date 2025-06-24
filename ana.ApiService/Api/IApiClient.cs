using System.Security.Claims;
using System.Security.Principal;

public interface IApiClient
{
    Task<CreateGroupResponse> CreateGroupAsync(string userId, string groupName);

    Task<List<AnaGroup>> GetGroupsAsync(string userId);

    Task SelectGroupAsync(string userId, string groupId);

    Task<AnaGroup> GetSelectedGroupAsync(string userId);

    Task UpdateUserSettingsAsync(string userId, AnaUser userSettings);
}