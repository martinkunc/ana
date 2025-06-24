


using System.Security.Claims;
using System.Security.Principal;
using Duende.IdentityModel.Client;

public class ApiClient : IApiClient
{
    //private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseAddress;
    private readonly string _webAppClientSecret;

    //private readonly ITokenService _tokenService;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IHttpClientFactory httpClientFactory,
        string baseAddress,
        string clientSecret,
        ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _baseAddress = baseAddress;
        
        _webAppClientSecret = clientSecret;

        _logger = logger;
    }

    public async Task<CreateGroupResponse> CreateGroupAsync(string userId, string groupName)
    {
        HttpClient _httpClient = await GetHttpClient();
        _logger.LogInformation("Creating group with name: {groupName}", groupName);
        var res = await _httpClient.PostAsJsonAsync("api/v1/group", new CreateGroupRequest { userId=userId, Name = groupName });
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



    public async Task<List<AnaGroup>> GetGroupsAsync(string userId)
    {
        var _httpClient = await GetHttpClient();
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
        var _httpClient = await GetHttpClient();
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

    public async Task<AnaGroup> GetSelectedGroupAsync(string userId)
    {
        var _httpClient = await GetHttpClient();
        _logger.LogInformation("Getting selected groups");
        var encodedUserId = Uri.EscapeDataString(userId);
        var res = await _httpClient.GetAsync($"api/v1/user/select-group/{encodedUserId}");
        if (res.IsSuccessStatusCode)
        {
            var resG = await res.Content.ReadFromJsonAsync<AnaGroup>();
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

    public async Task UpdateUserSettingsAsync(string userId, AnaUser userSettings)
    {
        var _httpClient = await GetHttpClient();
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

    
    private async Task<HttpClient> GetHttpClient()
    {
        string token = await GetAccessToken();
        //var token = _tokenService.GenerateJwtToken(identity);
        var _httpClient = _httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _httpClient.BaseAddress = new Uri(_baseAddress);
        return _httpClient;
    }

    private async Task<string> GetAccessToken()
    {
        var client = _httpClientFactory.CreateClient();
        var disco = await client.GetDiscoveryDocumentAsync(_baseAddress);
        if (disco.IsError) throw new Exception(disco.Error);

        // Request token
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = Config.IdentityServer.ClientId.WebApp,
            ClientSecret = _webAppClientSecret,
            Scope = Config.Scopes.anaApi
        });
        if (tokenResponse.IsError) throw new Exception(tokenResponse.Error);
        var token = tokenResponse.AccessToken;
        if (token == null)
            throw new Exception("Token is null");
        return token;
    }
}