using Aspire.Hosting;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using ana.SharedNet;

var builder = DistributedApplication.CreateBuilder(args);
//IResourceBuilder<AzureCosmosDBResource> cosmosResource = null;
IResourceBuilder<IResourceWithConnectionString> localCosmosResource = null;
IResourceBuilder<Aspire.Hosting.AzureCosmosDBResource> cosmosDb = null;

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
if (isLocalCosmosDb)
{
    Console.WriteLine("Using Local Cosmos from connection string: ");
    // First, add the connection string to the Configuration
    builder.Configuration["ConnectionStrings:cosmos-db"] = connString;
    
    // In development, we can use a local Cosmos DB emulator
    localCosmosResource = builder.AddConnectionString("cosmos-db");  // Let it pull from Configuration
    cosmosResourceBuilder = localCosmosResource;
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
}

Console.WriteLine($"MY: Connection string: { connString}");


if (cosmosResourceBuilder == null)
{
    throw new InvalidOperationException("Cosmos DB resource is not configured. Please check your connection string or Azure Cosmos DB setup.");
}

var apiServiceBuilder = builder.AddProject<Projects.ana_ApiService>("apiservice")
    .WithReference(cosmosResourceBuilder)
    .WaitFor(cosmosResourceBuilder);

var variables = builder.Configuration.AsEnumerable()
        .Where(kvp => kvp.Key.StartsWith("ana-"))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
foreach (var variable in variables)
{
    Console.WriteLine($"MY: Variable: {variable.Key} = {variable.Value}");
    apiServiceBuilder.WithEnvironment(variable.Key, variable.Value);
}

var apiService = apiServiceBuilder.WithExternalHttpEndpoints();


var apiUrlHttps = apiService.GetEndpoint("https");

Console.WriteLine($"MY: API URL HTTPS: {apiUrlHttps}");


apiServiceBuilder.WithEnvironment("ASPNETCORE_EXTERNAL_URL", apiUrlHttps);

builder.AddNpmApp("reactapp", "../ana.react")
    .WithReference(apiService)
    .WaitFor(apiService)
    //.WithEnvironment("VITE_API_URL", apiService.Resource.GetEndpoints() GetHttpEndpointUrl())
    //.WithEnvironmentPrefix("VITE_")
    .WithEnvironment("services__apiservice__https__0", apiUrlHttps)
    .WithEnvironment("VITE_API_URL", apiUrlHttps)
    .WithEnvironment("SOME_TEST", "CONTENT")
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();


Console.WriteLine($"MY: Environment: {builder.Environment.EnvironmentName}");





builder.Build().Run();
