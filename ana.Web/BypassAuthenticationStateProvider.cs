using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ana.Web;

public sealed record LocalProdBypassFlag(bool Enabled);

public sealed class BypassAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationState _state;

    public BypassAuthenticationStateProvider(LocalProdBypassFlag flag)
    {
        if (flag.Enabled)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "local-bypass"),
                new Claim("preferred_username", "local-bypass"),
                new Claim(ClaimTypes.Role, "Tester"),
                new Claim("sub", "local-bypass"),
                new Claim("auth_source", "local-prod-bypass")
            }, "Bypass");
            _state = new AuthenticationState(new ClaimsPrincipal(identity));
        }
        else
        {
            _state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
}
