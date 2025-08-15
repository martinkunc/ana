using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ana.SharedNet;

var builder = DistributedApplication.CreateBuilder(args);

// Get the app mode from configuration
var appModeIsProd = (builder.Configuration["AppMode"] ?? "dev") == "prod";
Console.WriteLine($"MY: App mode: {appModeIsProd}");

IResourceBuilder<IResourceWithConnectionString> localCosmosResource;
IResourceBuilder<Aspire.Hosting.AzureCosmosDBResource> cosmosDb;

var isAspireManifestGeneration = builder.ExecutionContext.IsPublishMode;
Console.WriteLine($"MY: Aspire manifest generation: {isAspireManifestGeneration}");

// Config doesn't allow passing empty string as empty, so for empty password we set another key to true
string defaultAdminPasswordIsEmpty = string.Empty;
var defaultAdminPassword = builder.Configuration["DefaultAdminPassword"];
if (defaultAdminPassword != null && defaultAdminPassword == "")
{
    defaultAdminPasswordIsEmpty = true.ToString();
}

var connString = await builder.GetFromSecretsOrVault(Config.SecretNames.AnaDbConnectionString);

Console.WriteLine($"MY: Connection string: {connString}");

var isLocalCosmosDb = EnvExtensions.IsCosmosDbLocal(connString);
IResourceBuilder<IResourceWithConnectionString> cosmosResourceBuilder;
IResourceBuilder<Aspire.Hosting.Azure.AzureStorageResource> storage;
IResourceBuilder<Aspire.Hosting.Azure.AzureBlobStorageResource> blobs;
IResourceBuilder<Aspire.Hosting.Azure.AzureTableStorageResource> tables;

// During the Aspire manifest generation, use the reference to pre-deployed CosmosDb with conn. string from keyvault,
// otherwise the connection string from user secrets
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
// all our, prefixed user secret settings will be passed as environment variables to Api
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
if (!appModeIsProd)
{
    if (builder.Environment.IsDevelopment() && !isAspireManifestGeneration)
    {
        var launchProfile = !appModeIsProd ? "BlazorWeb" : "BlazorWeb-Prod";
        // Add Blazor WebAssembly app to Aspire host
        var webApp = builder.AddProject<Projects.ana_Web>("webapp", launchProfile)
            .WithReference(apiService)
            .WaitFor(apiService)
            .WithExternalHttpEndpoints()
            .WithEnvironment("ApiService__Url", apiUrlHttps);
        var webUrlHttps = webApp.GetEndpoint("https");
        apiServiceBuilder.WithEnvironment("WebApp__Url", webUrlHttps);
    }
}
else
{
    // Before running the container, it must be built, see scripts
    Console.WriteLine("Production mode: Using Docker container for Web app.");
    var webContainerBuilder = builder.AddContainer("webapp", "ana-web")
    .WithHttpEndpoint(port: 7003, targetPort: 80)
    .WithEnvironment("BLAZOR_PORT", "80")
    .WithEnvironment("services__apiservice__https__0", apiUrlHttps);

    var webAppUrl = webContainerBuilder.GetEndpoint("http");
    apiServiceBuilder.WithEnvironment("WebApp__Url", webAppUrl);
}

// Conditionally add React app based on mode
if (!appModeIsProd)
{
    Console.WriteLine("Development mode: Using npm dev server for React app.");
    var nodeBuilder = builder.AddNpmApp("reactapp", "../ana.react")
        .WithReference(apiService)
        .WaitFor(apiService)
        .WithEnvironment("VITE_PORT", "80")
        .WithEnvironment("services__apiservice__https__0", apiUrlHttps)
        .WithEnvironment("VITE_API_URL", apiUrlHttps)
        .WithEnvironment("BROWSER", "none")
        .PublishAsDockerFile();

    // use fixed port for local development, so that api could preconfigure the IdP and CORS
    if (builder.Environment.IsDevelopment() && !isAspireManifestGeneration)
    {
        Console.WriteLine("Setting VITE_PORT to 7004 for React app in development.");
        nodeBuilder.WithHttpEndpoint(port: 7004, env: "VITE_PORT");
        var reactAppUrl = nodeBuilder.GetEndpoint("http");
        apiServiceBuilder.WithEnvironment("ReactApp__Url", reactAppUrl);
    }

    // Set the internal Node port to 80, which AC uses for communication, but keep standard external https port
    // Setting affects infrastructure resources created by aspire
    if (isAspireManifestGeneration)
    {
        // In Aspire manifest generation, use standard HTTP port 80 for React app with nginx
        Console.WriteLine("Setting port 80 for React app in production (nginx).");
        nodeBuilder.WithHttpEndpoint(port: 80, name: "http", env: "VITE_PORT")
            .WithExternalHttpEndpoints();
    }
}
else
{
    Console.WriteLine("Production mode: Using Docker container for React app.");
    var reactContainerBuilder = builder.AddContainer("reactapp", "ana-react")
        .WithHttpEndpoint(port: 7004, targetPort: 80)
        .WithEnvironment("VITE_PORT", "80")
        .WithEnvironment("services__apiservice__https__0", apiUrlHttps)
        .WithEnvironment("VITE_API_URL", apiUrlHttps)
        .WithEnvironment("BROWSER", "none");

    var reactAppUrl = reactContainerBuilder.GetEndpoint("http");
    apiServiceBuilder.WithEnvironment("ReactApp__Url", reactAppUrl);
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