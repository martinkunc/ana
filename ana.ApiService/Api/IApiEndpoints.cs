using System.Security.Claims;

public interface IApiEndpoints
{
    Task<CreateGroupResponse> CreateGroup(CreateGroupRequest request);
    Task<GetUserGroupsResponse> GetUserGroups(string userId);

    Task SelectGroup(string userId, string groupId);

    Task<AnaGroup> GetSelectedGroup(string userId);

    Task<List<AnaAnniv>> GetAnniversaries(string groupId);

    Task<CreateAnniversaryResponse> CreateAnniversary(string groupId, AnaAnniv anniversary);

    Task<AnaAnniv> UpdateAnniversary(string groupId, string anniversaryId, AnaAnniv anniversary);

    Task DeleteAnniversary(string anniversaryId, string groupId);

    Task<AnaUser> GetUserSettings(string userId);

    Task UpdateUserSettings(string userId, AnaUser userSettings);
}