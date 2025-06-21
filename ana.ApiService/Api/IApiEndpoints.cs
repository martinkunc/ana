using System.Security.Claims;

public interface IApiEndpoints
{
    Task<CreateGroupResponse> CreateGroup(CreateGroupRequest request);
    Task<GetUserGroupsResponse> GetUserGroups(string userId);
}