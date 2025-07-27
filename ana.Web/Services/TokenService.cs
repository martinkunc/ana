using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Timers;


public class TokenService : ITokenService, IDisposable
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<TokenService> _logger;
    private readonly NavigationManager _navigationManager;
    private readonly System.Timers.Timer _tokenCheckTimer;
    private string _cachedToken;
    private DateTimeOffset _tokenExpiry;

    public event EventHandler<TokenExpiredEventArgs> TokenExpired;

    public TokenService(
        IAccessTokenProvider tokenProvider,
        AuthenticationStateProvider authStateProvider,
        ILogger<TokenService> logger,
        NavigationManager navigationManager)
    {
        _tokenProvider = tokenProvider;
        _authStateProvider = authStateProvider;
        _logger = logger;
        _navigationManager = navigationManager;

        // Check token every 30 seconds
        _tokenCheckTimer = new System.Timers.Timer(30000);
        _tokenCheckTimer.Elapsed += OnTokenCheckTimerElapsed;
        _tokenCheckTimer.AutoReset = true;
        _tokenCheckTimer.Start();

        _logger.LogInformation("TokenService initialized with automatic token checking");
    }

    private async void OnTokenCheckTimerElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            if (await IsTokenExpiringSoonAsync())
            {
                _logger.LogWarning("Token is expiring soon, attempting refresh");
                await RefreshTokenAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic token check");
        }
    }

    public async Task<string> GetValidAccessTokenAsync()
    {
        // Check if we have a cached valid token
        if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _cachedToken;
        }

        return await RefreshTokenAsync();
    }

    public async Task<bool> IsTokenExpiringSoonAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (!authState.User.Identity.IsAuthenticated)
                return true;

            // Get the token directly to check expiration
            var tokenResult = await _tokenProvider.RequestAccessToken();
            if (tokenResult.TryGetToken(out var token))
            {
                var expClaim = ParseTokenClaims(token.Value)
                    .FirstOrDefault(c => c.Type == "exp");
                
                if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
                {
                    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                    var futureTime = DateTimeOffset.UtcNow.AddMinutes(5);
                    var isExpiringSoon = expDateTime <= futureTime;

                    if (isExpiringSoon)
                    {
                        _logger.LogWarning($"Token expires at {expDateTime}, which is soon");
                    }

                    return isExpiringSoon;
                }
            }

            // If we can't get the token or parse expiration, assume it's expiring
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token expiration");
            return true;
        }
    }

    private async Task<string> RefreshTokenAsync()
    {
        try
        {
            var tokenResult = await _tokenProvider.RequestAccessToken();

            if (tokenResult.TryGetToken(out var token))
            {
                _cachedToken = token.Value;

                // Parse token to get expiry
                var expClaim = ParseTokenClaims(token.Value)
                    .FirstOrDefault(c => c.Type == "exp");

                if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
                {
                    _tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(exp);
                    _logger.LogInformation($"Token refreshed, expires at {_tokenExpiry}");
                }

                return token.Value;
            }

            if (tokenResult.Status == AccessTokenResultStatus.RequiresRedirect)
            {
                _logger.LogWarning("Token refresh requires redirect");
                TokenExpired?.Invoke(this, new TokenExpiredEventArgs
                {
                    Message = "Authentication required",
                    RequiresRedirect = true
                });

                throw new AccessTokenNotAvailableException(
                    _navigationManager, tokenResult, null); 
            }

            throw new Exception("Unable to retrieve valid access token");
        }
        catch (AccessTokenNotAvailableException)
        {
            _cachedToken = null;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            _cachedToken = null;

            TokenExpired?.Invoke(this, new TokenExpiredEventArgs
            {
                Message = "Token refresh failed",
                RequiresRedirect = false
            });

            throw;
        }
    }

    private IEnumerable<Claim> ParseTokenClaims(string jwt)
    {
        try
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();
            foreach (var kvp in keyValuePairs)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
            }

            return claims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing token claims");
            return Enumerable.Empty<Claim>();
        }
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public void Dispose()
    {
        _tokenCheckTimer?.Stop();
        _tokenCheckTimer?.Dispose();
    }
}
