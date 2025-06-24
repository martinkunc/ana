


using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class ApiClient : IApiClient
{
    //private readonly string _baseAddress;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILogger<ApiClient> _logger;

    private readonly IHttpClientFactory _httpClientFactory;

    public ApiClient(IHttpClientFactory httpClientFactory,
        IAccessTokenProvider tokenProvider,
        AuthenticationStateProvider authenticationStateProvider,
        //string baseAddress,
        ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _authenticationStateProvider = authenticationStateProvider;
        //_baseAddress = baseAddress;
        _logger = logger;
    }

    public async Task<AnaGroup> CreateGroupAsync(string userId, string groupName)
    {
        var _httpClient = await GetHttpClient();

        var re = new CreateGroupRequest { userId = userId, Name = groupName };
        var r = await _httpClient.PostAsJsonAsync($"api/v1/group", re);
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Group created successfully for user {userId} with name {groupName}", userId, groupName);
            var resG = await r.Content.ReadFromJsonAsync<CreateGroupResponse>();
            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            _logger.LogInformation("Group created successfully: " + resG);
            return resG.Group;
        }
        else
        {
            _logger.LogError("Failed to create group for user {userId}, Status Code: {statusCode}", userId, r.StatusCode);
            throw new Exception($"Failed to create group: {r.ReasonPhrase}");
        }
    }


    public async Task<List<AnaGroup>> GetGroupsAsync(string userId)
    {
        var _httpClient = await GetHttpClient();

        // api/v1/user/groups
        //var userId = authState.User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
        var r = await _httpClient.GetAsync($"api/v1/user/groups/{encodedUserId}");
        _logger.LogInformation(r.StatusCode.ToString());
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Groups retrieved successfully for user {userId}", userId);
            var resG = await r.Content.ReadFromJsonAsync<GetGroupsResponse>();
            if (resG == null)
            {
                _logger.LogError("Failed to deserialize groups response");
                throw new Exception("Failed to deserialize groups response");
            }
            _logger.LogInformation("Groups retrieved successfully: {groupsCount} groups found", resG.Groups);
            return resG.Groups;
        }
        else
        {
            _logger.LogError("Failed to get user groups, Status Code: {statusCode}", r.StatusCode);
            throw new Exception($"Failed to get groups: {r.ReasonPhrase}");
        }
    }

    public async Task SelectGroupAsync(string userId, string groupId)
    {
        var _httpClient = await GetHttpClient();
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
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

    public async Task<AnaGroup> GetSelectedGroupAsync(string userId)
    {
        var _httpClient = await GetHttpClient();

        // api/v1/user/groups
        //var userId = authState.User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
        var r = await _httpClient.GetAsync($"api/v1/user/select-group/{encodedUserId}");
        _logger.LogInformation(r.StatusCode.ToString());
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("Selected group retrieved successfully for user {userId}", userId);
            _logger.LogInformation("Response: {response}", r);
            var resG = await r.Content.ReadFromJsonAsync<AnaGroup>();
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

    public async Task<List<AnaAnniv>> GetAnniversariesAsync(string groupId)
    {
        var _httpClient = await GetHttpClient();
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
        var _httpClient = await GetHttpClient();
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
            _logger.LogInformation("Ann9i created successfully: {resG}", resG);
            return resG;
        }
        else
        {
            _logger.LogError("Failed to create anniversary for group {groupId}, Status Code: {statusCode}", groupId, r.StatusCode);
            throw new Exception($"Failed to create anniversary: {r.ReasonPhrase}");
        }
    }

    public async Task<AnaAnniv> UpdateAnniversaryAsync(AnaAnniv anniversary)
    {
        var _httpClient = await GetHttpClient();
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
        var _httpClient = await GetHttpClient();
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

    public async Task<AnaUser> GetUserSettingsAsync(string userId)
    {
        var _httpClient = await GetHttpClient();
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
        var _httpClient = await GetHttpClient();
        var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
        
        var r = await _httpClient.PutAsJsonAsync($"api/v1/user/{encodedUserId}", userSettings);
        if (r.IsSuccessStatusCode)
        {
            _logger.LogInformation("AnaUser updated successfully ");
        }
        else
        {
            _logger.LogError("Failed to update AnaUser  {userId}, Status Code: {statusCode}", userId, r.StatusCode);
            throw new Exception($"Failed to update anaUser: {r.ReasonPhrase}");
        }
    }

    private async Task<HttpClient> GetHttpClient()
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();
        _logger.LogInformation("token request created...");

        if (tokenResult.TryGetToken(out var token))
        {
            var client = _httpClientFactory.CreateClient("Auth");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);
            _logger.LogInformation($"Token :{token.Value}");
            _logger.LogInformation(client.BaseAddress.ToString());
            return client;
        }
        else
        {
            _logger.LogError("Failed to retrieve access token ");
            throw new Exception("Failed to retrieve access token");
        }
    }
}