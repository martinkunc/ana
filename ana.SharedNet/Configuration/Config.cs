
namespace ana.SharedNet;
public partial class Config
{
    public static class Database
    {
        public const string Name = "ana-db";
    }

    public static class KeyVault
    {
        public const string KeyVaultUrl = "https://ana-kv.vault.azure.net/";

    }

    public static class SecretNames
    {
        public const string AnaDbConnectionString = "ana-db-connectionstring";
        public const string DefaultAdminPassword = "ana-default-admin-password";
        public const string DefaultAdminPasswordIsEmpty = "ana-default-admin-password-is-empty";

        public const string WebAppClientSecret = "ana-webapp-clientsecret";
        public const string BlazorClientSecret = "ana-blazor-clientsecret";

        public const string FromEmail = "ana-from-email";
        public const string SendGridKey = "ana-sendgrid-key";

        public const string TwilioAccountSid = "ana-twilio-accountsid";
        public const string TwilioAccountToken = "ana-twilio-accounttoken";
        public const string WhatsAppFrom = "ana-whatsapp-from";

    }



    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    
    
}