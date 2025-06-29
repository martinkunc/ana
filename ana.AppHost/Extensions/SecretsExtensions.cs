using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Hosting;
using ana.SharedNet;

public static class SecretsExtensions
{

    public static async Task<string> GetFromSecretsOrVault(
        this IDistributedApplicationBuilder builder, string secretKeyName)
    {
        var secretValue = builder.Configuration[secretKeyName];

        Console.WriteLine($"Secret {secretKeyName} from config {secretValue}");

        if (string.IsNullOrEmpty(secretValue))
        {
            if (builder.Environment.IsDevelopment()) {
                throw new InvalidOperationException($"Secret {secretKeyName} has to be configured as a secret in local development environment.");
            }
            var client = new SecretClient(new Uri(Config.KeyVault.KeyVaultUrl), new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(secretKeyName);
            secretValue = secret.Value;
        }

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new InvalidOperationException($"{secretKeyName} is not configured.");
        }

        return secretValue;
    }
}