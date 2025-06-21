using System.Security.Claims;
using System.Security.Principal;

public interface IApiClient
{
    Task CreateGroupAsync(IIdentity identity, string groupName);

    Task<List<AnaGroup>> GetGroupsAsync(IIdentity identity, string userId);
}