using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ana.Web.Services;

public class DummyTokenService : ITokenService
{
    private readonly string _token;
    private readonly DateTimeOffset _expiry;

    public event EventHandler<TokenExpiredEventArgs>? TokenExpired;

    public DummyTokenService()
    {
        _expiry = DateTimeOffset.UtcNow.AddDays(1);
        _token = BuildFakeJwt(_expiry);
    }

    public Task<string> GetValidAccessTokenAsync() => Task.FromResult(_token);

    public Task<bool> IsTokenExpiringSoonAsync()
        => Task.FromResult(_expiry <= DateTimeOffset.UtcNow.AddMinutes(5));

    private static string BuildFakeJwt(DateTimeOffset expiry)
    {
        string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "none", typ = "JWT" })));
        var payloadObj = new
        {
            sub = "local-bypass",
            name = "local-bypass",
            preferred_username = "local-bypass",
            role = "Tester",
            auth_source = "local-prod-bypass",
            exp = expiry.ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            iss = "local",
            aud = "ana_api"
        };
        var payload = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payloadObj)));
        var signature = ""; // empty because alg=none
        return string.Join('.', header, payload, signature);
    }
}
