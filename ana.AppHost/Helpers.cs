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
                return endpoint.Contains("localhost") || endpoint.Contains("127.0.0.1");
            }
        }
        return false;
    }
}