using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ana.SharedNet;
public static class SecretsExtensions
{
    public static async Task<string> GetFromSecretsOrVault(
        this WebAssemblyHostBuilder builder, string configKeyName, string keyVaultSecretName)
    {
        var secretValue = builder.Configuration[configKeyName];

        Console.WriteLine($"Secret {configKeyName} from config {secretValue}");

        if (string.IsNullOrEmpty(secretValue))
        {
            if (builder.HostEnvironment.IsDevelopment()) {
                throw new InvalidOperationException($"Secret {configKeyName} has to be configured as a secret in local development environment.");
            }
            var client = new SecretClient(new Uri(Config.KeyVault.KeyVaultUrl), new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(keyVaultSecretName);
            secretValue = secret.Value;
        }

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new InvalidOperationException("Cosmos DB connection string is not configured.");
        }

        return secretValue;
    }
}