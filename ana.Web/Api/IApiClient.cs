using System.Security.Claims;
using System.Security.Principal;

public interface IApiClient
{
    Task<AnaGroup> CreateGroupAsync(string userId, string groupName);

    Task<List<AnaGroup>> GetGroupsAsync(string userId);

    Task<AnaGroup> GetSelectedGroupAsync(string userId);

    Task SelectGroupAsync(string userId, string groupId);

    Task<List<AnaAnniv>> GetAnniversariesAsync(string groupId);

    Task<AnaAnniv> CreateAnniversaryAsync(string groupId, AnaAnniv anniversary);

    Task DeleteAnniversaryAsync(string anniversaryId, string groupId);

    Task<AnaAnniv> UpdateAnniversaryAsync(AnaAnniv anniversary);

    Task<AnaUser> GetUserSettingsAsync(string userId);

    Task UpdateUserSettingsAsync(string userId, AnaUser userSettings);
}