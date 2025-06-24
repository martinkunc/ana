

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

    public static class PreferredNotifications
    {
        public const string None = "None";
        public const string Email = "Email";
        public const string WhatsApp = "WhatsApp";
    }

    public static class KeyVault
    {
        public const string KeyVaultUrl = "https://ana-kv.vault.azure.net/";
        public const string ConnectionStringSecretName = "ana-db-connectionstring";
        public const string IssuerSigningKeySecretName = "issuer-signing-key";

    }

    // public static class IdentityServer
    // {
    //     public const string CertificateName = "anaidentitycert";
    //     public const string IssuerName = "Ana Identity Server";
    //     public const string AudienceName = "ana api";
    // }
    
    // public static class Resources
    // {
    //     public const string ana = "ana";
    //     // Add other resources as needed
    // }

    // public static class Scopes
    // {
    //     public const string ana = "ana";
    //     // Add other scopes as needed
    // }

    
    
}