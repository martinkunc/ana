


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
        var tokenResult = await _tokenProvider.RequestAccessToken();
        _logger.LogInformation("token request created...");
        // var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        // _logger.LogInformation($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        //var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        if (tokenResult.TryGetToken(out var token))
        {
            var client = _httpClientFactory.CreateClient("Auth");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);
            _logger.LogInformation($"Token :{token.Value}");
            _logger.LogInformation(client.BaseAddress.ToString());
            var re = new CreateGroupRequest { userId = userId, Name = groupName };
            var r = await client.PostAsJsonAsync($"api/v1/group", re);
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
        else
        {
            _logger.LogError("Failed to retrieve access token for user {userId}", userId);
            throw new Exception("Failed to retrieve access token");
        }

        // var token = _tokenProvider.GenerateJwtToken(identity);

        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        // _httpClient.BaseAddress = new Uri(_baseAddress);
        // _logger.LogInformation("Creating group with name: {groupName}", groupName);
        // var res = await _httpClient.PostAsJsonAsync("api/v1/group", new CreateGroupRequest { Name = groupName });
        // if (res.IsSuccessStatusCode)
        // {
        //     _logger.LogInformation("Group created successfully: {groupName}", groupName);
            
        // }
        // else
        // {
        //     _logger.LogError("Failed to create group: {groupName}, Status Code: {statusCode}",
        //         groupName, res.StatusCode);
        //     throw new Exception($"Failed to create group: {res.ReasonPhrase}");
        // }
    }

    public async Task<List<AnaGroup>> GetGroupsAsync(string userId)
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();
        _logger.LogInformation("token request created...");
        // var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        // _logger.LogInformation($"User is authenticated: {string.Join(",", authState.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        //var userId = authState.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");

        if (tokenResult.TryGetToken(out var token))
        {
            var client = _httpClientFactory.CreateClient("Auth");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);
            _logger.LogInformation($"Token :{token.Value}");
            _logger.LogInformation(client.BaseAddress.ToString());
            // api/v1/user/groups
            //var userId = authState.User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims.");
            var encodedUserId = System.Net.WebUtility.UrlEncode(userId);
            var r = await client.GetAsync($"api/v1/user/groups/{encodedUserId}");
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
        else
        {
            _logger.LogError("Failed to retrieve access token for user {userId}", userId);
            throw new Exception("Failed to retrieve access token");
        }
        
    }
}