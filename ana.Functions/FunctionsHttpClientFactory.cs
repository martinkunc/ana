using ana.SharedNet;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;

public class FunctionsHttpClientFactory : IAnaHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseAddress;
    private readonly string _webAppClientSecret;
    private readonly ILogger<FunctionsHttpClientFactory> _logger;

    public FunctionsHttpClientFactory(IHttpClientFactory httpClientFactory,
        string baseAddress,
        string clientSecret,
        ILogger<FunctionsHttpClientFactory> logger)
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
            ClientId = Config.IdentityServer.ClientId.WebApp,
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