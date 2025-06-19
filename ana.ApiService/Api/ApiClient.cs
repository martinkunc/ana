


using System.Security.Claims;
using System.Security.Principal;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseAddress;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient,
        string baseAddress,
        ITokenService tokenService,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _baseAddress = baseAddress;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task CreateGroupAsync(IIdentity identity,  string groupName)
    {

        var token = _tokenService.GenerateJwtToken(identity);
        
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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
}