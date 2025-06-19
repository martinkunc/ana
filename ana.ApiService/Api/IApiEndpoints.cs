using System.Security.Claims;

public interface IApiEndpoints
{
    Task<CreateGroupResponse> CreateGroup(ClaimsPrincipal user, CreateGroupRequest request);
}