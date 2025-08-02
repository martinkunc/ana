using Aspire.Hosting;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using ana.SharedNet;
using k8s.Models;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> localCosmosResource = null;
IResourceBuilder<Aspire.Hosting.AzureCosmosDBResource> cosmosDb = null;

var isAspireManifestGeneration = builder.ExecutionContext.IsPublishMode;
Console.WriteLine($"MY: Aspire manifest generation: {isAspireManifestGeneration}");

string defaultAdminPasswordIsEmpty = string.Empty;
var defaultAdminPassword = builder.Configuration["DefaultAdminPassword"];
if (defaultAdminPassword != null && defaultAdminPassword == "")
{
    defaultAdminPasswordIsEmpty = true.ToString();
}

var connString = await builder.GetFromSecretsOrVault(Config.SecretNames.AnaDbConnectionString);

Console.WriteLine($"MY: Connection string: {connString}");

var isLocalCosmosDb = EnvExtensions.IsCosmosDbLocal(connString);
IResourceBuilder<IResourceWithConnectionString> cosmosResourceBuilder = null;
IResourceBuilder<Aspire.Hosting.Azure.AzureStorageResource> storage = null;
IResourceBuilder<Aspire.Hosting.Azure.AzureBlobStorageResource> blobs = null;
IResourceBuilder<Aspire.Hosting.Azure.AzureTableStorageResource> tables = null;

if (isLocalCosmosDb && !isAspireManifestGeneration)
{
    Console.WriteLine("Using Local Cosmos from connection string: ");
    // First, add the connection string to the Configuration
    builder.Configuration["ConnectionStrings:cosmos-db"] = connString;

    // In development, we can use a local Cosmos DB emulator
    localCosmosResource = builder.AddConnectionString("cosmos-db");  // Let it pull from Configuration
    cosmosResourceBuilder = localCosmosResource;
    storage = builder.AddAzureStorage("storage").RunAsEmulator();
    blobs = storage.AddBlobs("blobs");
    tables = storage.AddTables("tables");
}
else
{
    Console.WriteLine("Using deployed Cosmos Db");
    // In production, we use the actual Azure Cosmos DB
    cosmosDb = builder.AddAzureCosmosDB("cosmos-db").ConfigureInfrastructure((infra) =>
    {
        var account = infra.GetProvisionableResources()
                                       .OfType<CosmosDBAccount>()
                                       .Single();

        account.Kind = CosmosDBAccountKind.GlobalDocumentDB;
        account.ConsistencyPolicy = new()
        {
            DefaultConsistencyLevel = DefaultConsistencyLevel.Session,
        };
        account.DatabaseAccountOfferType = CosmosDBAccountOfferType.Standard;

        account.Capabilities.Add(new BicepValue<CosmosDBAccountCapability>(new CosmosDBAccountCapability()
        {
            Name = "EnableServerless"
        }));

    });
    cosmosResourceBuilder = cosmosDb;
    storage = builder.AddAzureStorage("storage");
    blobs = storage.AddBlobs("blobs");
    tables = storage.AddTables("tables");
}

Console.WriteLine($"MY: Connection string: {connString}");


if (cosmosResourceBuilder == null)
{
    throw new InvalidOperationException("Cosmos DB resource is not configured. Please check your connection string or Azure Cosmos DB setup.");
}
if (storage == null || blobs == null || tables == null)
{
    throw new InvalidOperationException("Azure storage for Azure functions has to be set.");
}

var apiServiceBuilder = builder.AddProject<Projects.ana_ApiService>("apiservice")
    .WithReference(cosmosResourceBuilder)
    .WaitFor(cosmosResourceBuilder);

// Only set Development environment for local development
if (!isAspireManifestGeneration)
{
    apiServiceBuilder.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
}

var variables = builder.Configuration.AsEnumerable()
        .Where(kvp => kvp.Key.StartsWith("ana-"))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
if (!isAspireManifestGeneration)
{
    Console.WriteLine("Not in Aspire manifest generation, setting environment variables.");
    foreach (var variable in variables)
    {
        Console.WriteLine($"MY: Variable: {variable.Key} = {variable.Value}");
        apiServiceBuilder.WithEnvironment(variable.Key, variable.Value);
    }
}
else
{
    Console.WriteLine("In Aspire manifest generation, not setting environment variables.");
}

var apiService = apiServiceBuilder.WithExternalHttpEndpoints();

var apiUrlHttps = apiService.GetEndpoint("https");

Console.WriteLine($"MY: API URL HTTPS: {apiUrlHttps}");

apiServiceBuilder.WithEnvironment("ASPNETCORE_EXTERNAL_URL", apiUrlHttps);

// Only attach as extra resource in development because of debugging
// in production Blazor app is hosted by api service
if (builder.Environment.IsDevelopment() && !isAspireManifestGeneration)
{
    // Add Blazor WebAssembly app to Aspire host
    var webApp = builder.AddProject<Projects.ana_Web>("webapp")
        .WithReference(apiService)
        .WaitFor(apiService)
        .WithExternalHttpEndpoints()
        .WithEnvironment("ApiService__Url", apiUrlHttps);
    var webUrlHttps = webApp.GetEndpoint("https");
    apiServiceBuilder.WithEnvironment("WebApp__Url", webUrlHttps);
}

var nodeBuilder = builder.AddNpmApp("reactapp", "../ana.react")
    .WithReference(apiService)
    .WaitFor(apiService)
    //.WithEnvironment("VITE_API_URL", apiService.Resource.GetEndpoints() GetHttpEndpointUrl())
    //.WithEnvironmentPrefix("VITE_")
    .WithEnvironment("services__apiservice__https__0", apiUrlHttps)
    .WithEnvironment("VITE_API_URL", apiUrlHttps)
    .WithEnvironment("SOME_TEST", "CONTENT")
    .WithEnvironment("BROWSER", "none")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

if (builder.Environment.IsDevelopment() && !isAspireManifestGeneration)
{
    Console.WriteLine("Setting VITE_PORT to 7004 for React app in development.");
    nodeBuilder.WithHttpEndpoint(port: 7004, env: "VITE_PORT");
    var reactAppUrl = nodeBuilder.GetEndpoint("http");
    //webUrlHttps
    apiServiceBuilder.WithEnvironment("ReactApp__Url", reactAppUrl);

}
else
{
    nodeBuilder.WithHttpEndpoint(env: "VITE_PORT");
}

var functions = builder.AddAzureFunctionsProject<Projects.ana_Functions>("functions")
       .WithReference(apiService)
       .WithReference(blobs)
       //.WithReference(tables)
       .WaitFor(storage)
       .WithEnvironment("ApiService__Url", apiUrlHttps)
       .WaitFor(apiService)
       .WithHostStorage(storage);

Console.WriteLine($"MY: Environment: {builder.Environment.EnvironmentName}");



builder.Build().Run();
