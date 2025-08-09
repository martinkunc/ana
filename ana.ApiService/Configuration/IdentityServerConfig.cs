using ana.SharedNet;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

public class IdentityServerConfig
{
    public static class IdentityServer
    {
        public const string CertificateName = "anaidentitycert";
        public const string AudienceName = "ana_api";
    }

    public static class Resources
    {
        public const string ana = "ana";
    }

    public static IEnumerable<ApiResource> GetApis()
    {
        return new List<ApiResource>
            {
                new ApiResource(Resources.ana, "Ana Service"),
                new ApiResource(Config.IdentityServer.Scopes.anaApi, "Ana API Service")
                {
                    Scopes = { Config.IdentityServer.Scopes.anaApi }
                }
            };
    }

    // ApiScope is used to protect the API 
    public static IEnumerable<ApiScope> GetApiScopes()
    {
        return new List<ApiScope>
            {
                new ApiScope(Config.IdentityServer.Scopes.ana, "ana Service"), // blazor
                new ApiScope(Config.IdentityServer.Scopes.anaApi, "ana Api Service"),
            };
    }

    // Identity resources are data like user ID, name, or email address of a user
    // see: http://docs.identityserver.io/en/release/configuration/resources.html
    public static IEnumerable<IdentityResource> GetResources()
    {
        return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };
    }

    // IdP Clients configuration
    public static IEnumerable<Client> GetClients(IConfiguration configuration, string apiUrl, List<string> webClientsUrls, string webAppClientSecret)
    {
        return new List<Client>
            {
                // Blazor + React use Authorization Code + PKCE OAuth flow (for SPAs/Blazor WebAssembly)
                new Client
                {
                    ClientId = Config.IdentityServer.ClientId.Blazor,
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,
                    RedirectUris = CreateRedirectUris(webClientsUrls, "/authentication/login-callback" ),
                    PostLogoutRedirectUris = CreatePostLogoutRedirectUris(apiUrl, webClientsUrls, "/account/login"),
                    AllowedCorsOrigins = new[] { apiUrl }.Concat(webClientsUrls).ToList(),
                    AllowedScopes =
                    [
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        Config.IdentityServer.Scopes.ana,
                        Config.IdentityServer.Scopes.anaApi,
                    ]
                },
                // Web in api is using Authorization Code + Client Credentials OAuth flow (for server-side web apps)
                new Client
                {
                    ClientId = Config.IdentityServer.ClientId.WebApp,
                    ClientName = "WebApp Client",
                    ClientSecrets =
                    [
                        new Secret(webAppClientSecret.Sha256())
                    ],
                    ClientUri = $"{configuration["WebAppClient"]}",
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    AllowAccessTokensViaBrowser = false,
                    RequireConsent = false,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    RequirePkce = false,
                    RedirectUris =
                    [
                        $"{configuration["WebAppClient"]}/signin-oidc"
                    ],
                    PostLogoutRedirectUris =
                    [
                        $"{configuration["WebAppClient"]}/signout-callback-oidc"
                    ],
                    AllowedScopes =
                    [
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        Config.IdentityServer.Scopes.ana,
                        Config.IdentityServer.Scopes.anaApi,
                    ],
                    AccessTokenLifetime = 60*60*2,
                    IdentityTokenLifetime= 60*60*2
                },
            };
    }

    private static ICollection<string> CreateRedirectUris(List<string> externalUris, string path)
    {
        var baseUrls = externalUris.Select(u => u + path);
        return baseUrls.ToList();
    }

    private static ICollection<string> CreatePostLogoutRedirectUris(string apiUrl, List<string> webClientsUris, string path)
    {
        return webClientsUris.Select(u => apiUrl + path + "?returnUrl=" + u).ToList();
    }
}