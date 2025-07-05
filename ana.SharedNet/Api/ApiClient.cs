using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

public class ApiClient : IApiClient
{
    private readonly IAnaHttpClientFactory _anaHttpClientFactory;

    //private readonly HttpClient _httpClient;


    //private readonly ITokenService _tokenService;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IAnaHttpClientFactory anaHttpClientFactory,
        ILogger<ApiClient> logger)
    {
        _anaHttpClientFactory = anaHttpClientFactory;

        _logger = logger;
    }

    public async Task<CreateGroupResponse> CreateGroupAsync(string userId, string groupName)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        _logger.LogInformation("Creating group with name: {groupName}", groupName);
        var res = await _httpClient.PostAsJsonAsync("api/v1/group", new CreateGroupRequest { userId = userId, Name = groupName });
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("Group created successfully: {groupName}", groupName);
            var resG = await res.Content.ReadFromJsonAsync<CreateGroupResponse>();
            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            return resG;
        }
        else
        {
            _logger.LogError("Failed to create group: {groupName}, Status Code: {statusCode}",
                groupName, res.StatusCode);
            throw new Exception($"Failed to create group: {res.ReasonPhrase}");
        }
    }

    public async Task CreateUserAsync(AnaUser user)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        _logger.LogInformation("Creating user with name: {DisplayName}", user.DisplayName);
        var res = await _httpClient.PostAsJsonAsync("api/v1/user", user);
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("User created successfully: {DisplayName}", user.DisplayName);
        }
        else
        {
            _logger.LogError("Failed to create user: {DisplayName}, Status Code: {statusCode}",
                user.DisplayName, res.StatusCode);
            throw new Exception($"Failed to create user: {res.ReasonPhrase}");
        }
    }

    public async Task CancelUserAsync(string userId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        _logger.LogInformation("Cancelling user: {Id}", userId);
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
        var res = await _httpClient.DeleteAsync($"api/v1/user/{encodedUserId}");
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("User cancelled successfully: {userId}", userId);
        }
        else
        {
            _logger.LogError("Failed to cancel user: {userId}, Status Code: {statusCode}",
                userId, res.StatusCode);
            throw new Exception($"Failed to cancel user: {res.ReasonPhrase}");
        }
    }

    public async Task<List<AnaAnniv>> GetAnniversariesAsync(string groupId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedGroupId = System.Net.WebUtility.UrlEncode(groupId);
        var r = await _httpClient.GetAsync($"api/v1/group/{encodedGroupId}/anniversaries");
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Selected group retrieved successfully for group {groupId}", groupId);
            _logger.LogInformation("Response: {response}", r);
            var resG = await r.Content.ReadFromJsonAsync<List<AnaAnniv>>();
            _logger.LogInformation("resg: {resG}", resG);

            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            _logger.LogInformation("Groups retrieved successfully: {groupsCount} groups found", resG);
            return resG;
        }
        else
        {
            _logger.LogError("Failed to get user groups, Status Code: {statusCode}", r.StatusCode);
            throw new Exception($"Failed to get groups: {r.ReasonPhrase}");
        }
    }

    public async Task<AnaAnniv> CreateAnniversaryAsync(string groupId, AnaAnniv anniversary)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedGroupId = System.Net.WebUtility.UrlEncode(groupId);
        var r = await _httpClient.PostAsJsonAsync($"api/v1/group/{encodedGroupId}/anniversary", anniversary);
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Anniversary created successfully for group {groupId}", groupId);
            var resG = await r.Content.ReadFromJsonAsync<AnaAnniv>();
            _logger.LogInformation("resg: {resG}", resG);

            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            _logger.LogInformation("Anni created successfully: {resG}", resG);
            return resG;
        }
        else
        {
            _logger.LogError("Failed to create anniversary for group {groupId}, Status Code: {statusCode}", groupId, r.StatusCode);
            throw new Exception($"Failed to create anniversary: {r.ReasonPhrase}");
        }
    }

    public async Task CreateGroupMemberAsync(string groupId, string email)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedGroupId = System.Net.WebUtility.UrlEncode(groupId);
        var newMember = new AnaGroupMember
        {
            GroupId = groupId,
            Role = AnaRoleNames.User,
            Email = email,
        };
        var r = await _httpClient.PostAsJsonAsync($"api/v1/group/{encodedGroupId}/member", newMember);
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Member created successfully for group {groupId}", groupId);
        }
        else
        {
            _logger.LogError("Failed to create member for group {groupId}, Status Code: {statusCode}", groupId, r.StatusCode);
            throw new Exception($"Failed to create member: {r.ReasonPhrase}");
        }
    }

    public async Task DeleteGroupMemberAsync(string groupId, string userId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        groupId = System.Net.WebUtility.UrlEncode(groupId);
        userId = System.Net.WebUtility.UrlEncode(userId);
        var r = await _httpClient.DeleteAsync($"api/v1/group/{groupId}/member/{userId}");
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Member deleted successfully from group {groupId}", groupId);
        }
        else
        {
            _logger.LogError("Failed to delete member from group {groupId}, Status Code: {statusCode}", groupId, r.StatusCode);
            throw new Exception($"Failed to delete member: {r.ReasonPhrase}");
        }
    }


    public async Task<List<AnaGroupMember>> GetGroupMembersAsync(string groupId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();

        _logger.LogInformation("Getting group members for group {groupId}", groupId);
        var encodedGroupId = Uri.EscapeDataString(groupId);

        var res = await _httpClient.GetAsync($"api/v1/group/{encodedGroupId}/members");
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("Group members obtained successfully");
            var resG = await res.Content.ReadFromJsonAsync<List<AnaGroupMember>>();
            _logger.LogInformation("resg: {resG}", resG);

            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            _logger.LogInformation("Groups retrieved successfully: {groupsCount} groups found", resG);
            return resG;
        }
        else
        {
            _logger.LogError("Failed to get Group members {groupId}, Status Code: {statusCode}", groupId, res.StatusCode);
            throw new Exception($"Failed to get group members: {res.ReasonPhrase}");
        }
    }


    public async Task<AnaAnniv> UpdateAnniversaryAsync(AnaAnniv anniversary)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedGroupId = System.Net.WebUtility.UrlEncode(anniversary.GroupId);
        var encodedId = System.Net.WebUtility.UrlEncode(anniversary.Id);
        var r = await _httpClient.PutAsJsonAsync($"api/v1/group/{encodedGroupId}/anniversary/{encodedId}", anniversary);
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Anniversary updated successfully ");
            var resG = await r.Content.ReadFromJsonAsync<AnaAnniv>();
            _logger.LogInformation("resg: {resG}", resG);

            if (resG == null)
            {
                _logger.LogError("Failed to deserialize AnaAnniv response");
                throw new Exception("Failed to deserialize AnaAnniv response");
            }
            _logger.LogInformation("AnaAnni updated successfully: {resG}", resG);
            return resG;
        }
        else
        {
            _logger.LogError("Failed to update anniversary for group {groupId}, Status Code: {statusCode}", anniversary.GroupId, r.StatusCode);
            throw new Exception($"Failed to update anniversary: {r.ReasonPhrase}");
        }
    }

    public async Task DeleteAnniversaryAsync(string anniversaryId, string groupId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedGroupId = System.Net.WebUtility.UrlEncode(groupId);
        var encodedAnniversaryId = System.Net.WebUtility.UrlEncode(anniversaryId);
        var r = await _httpClient.DeleteAsync($"api/v1/group/{encodedGroupId}/anniversary/{encodedAnniversaryId}");
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Anniversary deleted successfully ");
        }
        else
        {
            _logger.LogError("Failed to delete anniversary for group {groupId}, Status Code: {statusCode}", groupId, r.StatusCode);
            throw new Exception($"Failed to update anniversary: {r.ReasonPhrase}");
        }
    }

    public async Task<List<AnaGroup>> GetGroupsAsync(string userId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        _logger.LogInformation("Getting groups");
        var encodedGuid = Uri.EscapeDataString(userId);
        var res = await _httpClient.GetAsync($"api/v1/user/groups/{encodedGuid}");
        if (res.IsSuccessStatusCode)
        {
            var resG = await res.Content.ReadFromJsonAsync<GetUserGroupsResponse>();
            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            return resG.Groups;
        }
        else
        {
            _logger.LogError("Failed to get user groups, Status Code: {statusCode}",
                res.StatusCode);
            throw new Exception($"Failed to get group: {res.ReasonPhrase}");
        }
    }

    public async Task SelectGroupAsync(string userId, string groupId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        _logger.LogInformation("Selecting group {groupId} for user {userId}", groupId, userId);
        var encodedUserId = Uri.EscapeDataString(userId);
        var encodedGroupId = Uri.EscapeDataString(groupId);
        var res = await _httpClient.PostAsync($"api/v1/user/select-group/{encodedUserId}/{encodedGroupId}", null);
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("Group {groupId} selected successfully for user {userId}", groupId, userId);
        }
        else
        {
            _logger.LogError("Failed to select group: {groupId} for user: {userId}, Status Code: {statusCode}",
                groupId, userId, res.StatusCode);
            throw new Exception($"Failed to select group: {res.ReasonPhrase}");
        }
    }

    public async Task<GetSelectedGroupResponse?> GetSelectedGroupAsync(string userId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        _logger.LogInformation("Getting selected groups");
        var encodedUserId = Uri.EscapeDataString(userId);
        var res = await _httpClient.GetAsync($"api/v1/user/select-group/{encodedUserId}");
        if (res.IsSuccessStatusCode)
        {
            var resG = await res.Content.ReadFromJsonAsync<GetSelectedGroupResponse>();
            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            return resG;
        }
        else
        {
            _logger.LogError("Failed to get user groups, Status Code: {statusCode}",
                res.StatusCode);
            throw new Exception($"Failed to get group: {res.ReasonPhrase}");
        }
    }


    public async Task<AnaUser> GetUserSettingsAsync(string userId)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
        var r = await _httpClient.GetAsync($"api/v1/user/{encodedUserId}");
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("User settings retrieved for user {userId}", userId);
            _logger.LogInformation("Response: {response}", r);
            var resG = await r.Content.ReadFromJsonAsync<AnaUser>();
            _logger.LogInformation("resg: {resG}", resG);

            if (resG == null)
            {
                _logger.LogError("Failed to deserialize AnaUser response");
                throw new Exception("Failed to deserialize AnaUser response");
            }
            _logger.LogInformation("AnaUser retrieved successfully: {resG} ", resG);
            return resG;
        }
        else
        {
            _logger.LogError("Failed to get user settings, Status Code: {statusCode}", r.StatusCode);
            throw new Exception($"Failed to get user settings: {r.ReasonPhrase}");
        }
    }

    public async Task UpdateUserSettingsAsync(string userId, AnaUser userSettings)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();

        _logger.LogInformation("Updating settings for user {userId}", userId);
        var encodedUserId = Uri.EscapeDataString(userId);
        var res = await _httpClient.PutAsJsonAsync($"api/v1/user/{encodedUserId}", userSettings);
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("AnaUser updated successfully");
        }
        else
        {
            _logger.LogError("Failed to update AnaUser  {userId}, Status Code: {statusCode}", userId, res.StatusCode);
            throw new Exception($"Failed to update anaUser: {res.ReasonPhrase}");
        }
    }

    public async Task ChangeGroupMemberRoleAsync(string groupId, string userId, string roleName)
    {
        HttpClient _httpClient = await _anaHttpClientFactory.GetHttpClient();
        var encodedGroupId = System.Net.WebUtility.UrlEncode(groupId);
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
        var req = new ChangeGroupMemberRoleRequest { RoleName = roleName };
        var r = await _httpClient.PutAsJsonAsync($"api/v1/group/{encodedGroupId}/member/{encodedUserId}/role", req);
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Group member role updated successfully ");
        }
        else
        {
            _logger.LogError("Failed to update group member for group {groupId}, Status Code: {statusCode}", groupId, r.StatusCode);
            throw new Exception($"Failed to update group member role: {r.ReasonPhrase}");
        }
    }

}