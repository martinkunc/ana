public static class EnvExtensions
{
    public static IResourceBuilder<T> WithEnvironmentPrefix<T>(this IResourceBuilder<T> resourceBuilder, string prefix)
        where T : IResourceWithEnvironment
    {
        return resourceBuilder.WithEnvironment(context =>
        {
            var kvps = context.EnvironmentVariables.ToArray();

            // Adds a prefix to all environment variable names
            foreach (var p in kvps)
            {
                context.EnvironmentVariables[$"{prefix}{p.Key}"] = p.Value;
            }
        });
    }

    public static bool IsCosmosDbLocal(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return false;

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountEndpoint=", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = part.Substring("AccountEndpoint=".Length).Trim();
                if (endpoint.Contains("localhost") || endpoint.Contains("127.0.0.1"))
                    return true;
                if (IsWslHostAddress(endpoint))
                    return true;
                if (endpoint.Contains("host.docker.internal") ||
                    endpoint.Contains(".local") ||
                    endpoint.Contains("emulator"))
                    return true;
            }
        }
        return false;
    }

    private static bool IsWslHostAddress(string endpoint)
    {
        try
        {
            var uri = new Uri(endpoint);
            var host = uri.Host;
            // Check if it's a private IP address that could be WSL host
            if (System.Net.IPAddress.TryParse(host, out var ip))
            {
                var bytes = ip.GetAddressBytes();

                if (bytes.Length == 4) // IPv4
                {
                    // 172.16.0.0/12 range (WSL commonly uses 172.x.x.x)
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        return true;

                    // 192.168.0.0/16 range
                    if (bytes[0] == 192 && bytes[1] == 168)
                        return true;

                    // 10.0.0.0/8 range
                    if (bytes[0] == 10)
                        return true;
                }
            }
            return false;
        }
        catch
        {
            // If URL parsing fails, fall back to string matching
            return endpoint.Contains("172.") || endpoint.Contains("192.168.") || endpoint.Contains("10.");
        }
    }
}