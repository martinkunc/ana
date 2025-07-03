using System.Security.Claims;

public interface IApiEndpoints
{
    Task<CreateGroupResponse> CreateGroup(CreateGroupRequest request);

    Task CreateUser(AnaUser user);
    Task<GetUserGroupsResponse> GetUserGroups(string userId);

    Task SelectGroup(string userId, string groupId);

    Task<GetSelectedGroupResponse?> GetSelectedGroup(string userId);
    Task<List<AnaGroupMember>> GetGroupMembers(string groupId);

    Task<List<AnaAnniv>> GetAnniversaries(string groupId);

    Task CreateGroupMember(string groupId, AnaGroupMember newMember);

    Task ChangeGroupMemberRole(string groupId, string userId, ChangeGroupMemberRoleRequest req);

    Task DeleteGroupMember(string groupId, string userId);
    Task<CreateAnniversaryResponse> CreateAnniversary(string groupId, AnaAnniv anniversary);

    Task<AnaAnniv> UpdateAnniversary(string groupId, string anniversaryId, AnaAnniv anniversary);

    Task DeleteAnniversary(string anniversaryId, string groupId);

    Task<AnaUser> GetUserSettings(string userId);

    Task UpdateUserSettings(string userId, AnaUser userSettings);

    Task DeleteUser(string userId);
}