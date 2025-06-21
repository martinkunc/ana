using System.Security.Claims;
using System.Security.Principal;

public interface IApiClient
{
    Task<AnaGroup> CreateGroupAsync(string userId, string groupName);

    Task<List<AnaGroup>> GetGroupsAsync(string userId);
}