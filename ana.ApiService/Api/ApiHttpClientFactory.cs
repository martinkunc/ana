using Duende.IdentityModel.Client;
using ana.SharedNet;
public class ApiHttpClientFactory : IAnaHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseAddress;
    private readonly string _webAppClientSecret;
    private readonly ILogger<ApiHttpClientFactory> _logger;

    public ApiHttpClientFactory(IHttpClientFactory httpClientFactory,
        string baseAddress,
        string clientSecret,
        ILogger<ApiHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _baseAddress = baseAddress;
        _webAppClientSecret = clientSecret;
        _logger = logger;
    }

    public async Task<HttpClient> GetHttpClient()
    {
        string token = await GetAccessToken();
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
            ClientId = ana.SharedNet.Config.IdentityServer.ClientId.WebApp,
            ClientSecret = _webAppClientSecret,
            Scope = Config.IdentityServer.Scopes.anaApi
        });
        if (tokenResponse.IsError) throw new Exception(tokenResponse.Error);
        var token = tokenResponse.AccessToken;
        if (token == null)
            throw new Exception("Token is null");
        return token;
    }
}