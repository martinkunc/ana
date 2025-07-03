
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
public class WebHttpClientFactory : IAnaHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseAddress;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly ILogger<WebHttpClientFactory> _logger;

    public WebHttpClientFactory(IHttpClientFactory httpClientFactory,
        string baseAddress,
        IAccessTokenProvider tokenProvider,
        ILogger<WebHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _baseAddress = baseAddress;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<HttpClient> GetHttpClient()
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