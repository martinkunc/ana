using System.Security.Claims;
using System.Security.Principal;

public interface IApiClient
{
    Task<CreateGroupResponse> CreateGroupAsync(string userId, string groupName);
    Task CreateUserAsync(AnaUser user);
    Task CancelUserAsync(string userId);
    Task<List<AnaGroup>> GetGroupsAsync(string userId);

    Task<GetSelectedGroupResponse?> GetSelectedGroupAsync(string userId);

    Task SelectGroupAsync(string userId, string groupId);

    Task<List<AnaAnniv>> GetAnniversariesAsync(string groupId);

    Task<AnaAnniv> CreateAnniversaryAsync(string groupId, AnaAnniv anniversary);

    Task DeleteAnniversaryAsync(string anniversaryId, string groupId);

    Task<AnaAnniv> UpdateAnniversaryAsync(AnaAnniv anniversary);

    Task<List<AnaGroupMember>> GetGroupMembersAsync(string groupId);

    Task<AnaUser> GetUserSettingsAsync(string userId);

    Task UpdateUserSettingsAsync(string userId, AnaUser userSettings);

    Task CreateGroupMemberAsync(string groupId, string email);

    Task ChangeGroupMemberRoleAsync(string groupId, string userId, string roleName);

    Task DeleteGroupMemberAsync(string groupId, string userId);

    Task RunDailyTasksAsync();
}