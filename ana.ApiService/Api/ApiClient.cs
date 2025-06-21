


using System.Security.Claims;
using System.Security.Principal;
using Duende.IdentityModel.Client;

public class ApiClient : IApiClient
{
    //private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseAddress;
    //private readonly ITokenService _tokenService;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IHttpClientFactory httpClientFactory,
        string baseAddress,
        
        ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _baseAddress = baseAddress;
        
        _logger = logger;
    }

    public async Task CreateGroupAsync(IIdentity identity, string groupName)
    {
        var client = _httpClientFactory.CreateClient();
        var disco = await client.GetDiscoveryDocumentAsync("https://localhost:7398");
        if (disco.IsError) throw new Exception(disco.Error);

        // Request token
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "webapp",
            ClientSecret = "secret",
            Scope = "ana_api"
        });
        if (tokenResponse.IsError) throw new Exception(tokenResponse.Error);
        //var token = _tokenService.GenerateJwtToken(identity);
        var _httpClient = _httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        _httpClient.BaseAddress = new Uri(_baseAddress);
        _logger.LogInformation("Creating group with name: {groupName}", groupName);
        var res = await _httpClient.PostAsJsonAsync("api/v1/group", new CreateGroupRequest { Name = groupName });
        if (res.IsSuccessStatusCode)
        {
            _logger.LogInformation("Group created successfully: {groupName}", groupName);
        }
        else
        {
            _logger.LogError("Failed to create group: {groupName}, Status Code: {statusCode}",
                groupName, res.StatusCode);
            throw new Exception($"Failed to create group: {res.ReasonPhrase}");
        }
    }
    
    public async Task<List<AnaGroup>> GetGroupsAsync(IIdentity identity, string userId)
    {

        var client = _httpClientFactory.CreateClient();
//  "https://localhost:7398"
        var disco = await client.GetDiscoveryDocumentAsync(_baseAddress);
        if (disco.IsError) throw new Exception(disco.Error);

        // Request token
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "webapp",
            ClientSecret = "secret",
            Scope = "ana_api"
        });
        if (tokenResponse.IsError) throw new Exception(tokenResponse.Error);

        //var token = _tokenService.GenerateJwtToken(identity);
        _logger.LogInformation($"Api Generated token: {tokenResponse.AccessToken}");
        
        var _httpClient = _httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        _httpClient.BaseAddress = new Uri(_baseAddress);
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
}