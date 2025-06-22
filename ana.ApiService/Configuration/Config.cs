using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.Azure.Cosmos;
using OpenTelemetry.Context;

public class Config
{
    public static class Database
    {
        public const string Name = "ana-db";
    }

    public static class SecretsKeyNames
    {
        public const string ConnectionStringCosmos = "ConnectionStrings:cosmos-db";
        public const string IssuerSigningKeySecretName = "issuer-signing-key";
    }

    public static class Users
    {
        public const string DefaultAdminPasswordKeyVaultSecretName = "default-admin-password";
    }

    public static class KeyVault
    {
        public const string KeyVaultUrl = "https://ana-kv.vault.azure.net/";
        public const string ConnectionStringSecretName = "ana-db-connectionstring";
        public const string IssuerSigningKeySecretName = "issuer-signing-key";

    }

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

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    // client want to access resources (aka scopes)
    public static IEnumerable<Client> GetClients(IConfiguration configuration, string externalUrl)
    {
        return new List<Client>
            {
                new Client
                {
                    ClientId = "blazor",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,

                    RedirectUris = {   $"{externalUrl}/authentication/login-callback" },
                    PostLogoutRedirectUris = { $"{externalUrl}/authentication/login" }, // $"{externalUrl}/authentication/logout-callback" 

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
                    ClientId = "webapp",
                    ClientName = "WebApp Client",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256()) // change
                    },
                    ClientUri = $"{configuration["WebAppClient"]}",                             // public uri of the client
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
}