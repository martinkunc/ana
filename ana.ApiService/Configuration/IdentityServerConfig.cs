using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.Azure.Cosmos;
using OpenTelemetry.Context;

public class IdentityServerConfig
{


    public static class IdentityServer
    {
        public const string CertificateName = "anaidentitycert";
        //public const string IssuerName = "Ana Identity Server";
        public const string AudienceName = "ana_api";

        public static class ClientId
        {
            public const string WebApp = "webapp";
            public const string Blazor = "blazor";
        }
    }
    
    public static class Resources
    {
        public const string ana = "ana";
        // Add other resources as needed
    }

    public static class Scopes
    {
        public const string ana = "ana";
        public const string anaApi = "ana_api";
        // Add other scopes as needed
    }

    // ApiResources define the apis in your system
    public static IEnumerable<ApiResource> GetApis()
    {
        return new List<ApiResource>
            {
                new ApiResource(Resources.ana, "Ana Service"),
                new ApiResource(Scopes.anaApi, "Ana API Service")
                {
                    Scopes = { Scopes.anaApi }
                }
            };
    }

    // ApiScope is used to protect the API 
    //The effect is the same as that of API resources in IdentityServer 3.x
    public static IEnumerable<ApiScope> GetApiScopes()
    {
        return new List<ApiScope>
            {
                new ApiScope(Scopes.ana, "ana Service"), // blazor
                new ApiScope(Scopes.anaApi, "ana Api Service"),
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





    // client want to access resources (aka scopes)
    public static IEnumerable<Client> GetClients(IConfiguration configuration, string[] externalUris, string webAppClientSecret)
    {
        return new List<Client>
            {
                new Client
                {
                    ClientId =IdentityServer.ClientId.Blazor,
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,

                    RedirectUris = CreateRedirectUris(externalUris, "/authentication/login-callback" ),
                    PostLogoutRedirectUris = CreateRedirectUris(externalUris, "/authentication/login" ),

                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        Scopes.ana,
                        Scopes.anaApi,
                    }
                },
                new Client
                {
                    ClientId = IdentityServer.ClientId.WebApp,
                    ClientName = "WebApp Client",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret(webAppClientSecret.Sha256())
                    },
                    ClientUri = $"{configuration["WebAppClient"]}", // public uri of the client
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    AllowAccessTokensViaBrowser = false,
                    RequireConsent = false,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    RequirePkce = false,
                    RedirectUris = new List<string>
                    {
                        $"{configuration["WebAppClient"]}/signin-oidc"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $"{configuration["WebAppClient"]}/signout-callback-oidc"
                    },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        Scopes.ana,
                        Scopes.anaApi,
                    },
                    AccessTokenLifetime = 60*60*2, // 2 hours
                    IdentityTokenLifetime= 60*60*2 // 2 hours
                },
            };
    }

    private static ICollection<string> CreateRedirectUris(string[] externalUris, string suffix)
    {
        return externalUris.Select(u => u + suffix).ToList();
    }
}