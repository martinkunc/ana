
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;


public class WebHttpClientFactory : IAnaHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseAddress;
    private readonly ITokenService _tokenService;
    private readonly ILogger<WebHttpClientFactory> _logger;

    public WebHttpClientFactory(IHttpClientFactory httpClientFactory,
        string baseAddress,
        ITokenService tokenService,
        ILogger<WebHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _baseAddress = baseAddress;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<HttpClient> GetHttpClient()
    {
        try
        {
            _logger.LogInformation("Requesting valid access token...");
            
            var token = await _tokenService.GetValidAccessTokenAsync();
            
            var client = _httpClientFactory.CreateClient("Auth");
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            _logger.LogInformation("HttpClient configured with valid token");
            _logger.LogInformation(client.BaseAddress?.ToString() ?? "No base address");
            
            return client;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            _logger.LogWarning("Access token not available, redirect required");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve access token");
            throw new Exception("Failed to retrieve access token", ex);
        }
    }

}