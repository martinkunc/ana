using Aspire.Hosting;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


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
var connString = builder.Configuration["ConnectionStrings:cosmos-db"];

Console.WriteLine($"MY: Connection string: { connString}");

if (connString != null)
{
    Console.WriteLine("Using Cosmos from connection string: ");
    // In development, we can use a local Cosmos DB emulator
    localCosmosResource = builder.AddConnectionString("cosmos-db");

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

        // ? location
        //account.AssignProperty(x => x.DatabaseAccountOfferType, $"'{CosmosDBAccountOfferType.Standard}'");
        //account.AssignProperty(x => x.Locations[0].LocationName, $"'{AzureLocation.UKWest.Name}'");
    });
    //cosmosDb = cr.AddCosmosDatabase("ana-db");
    var vaultUrl = "https://ana-kv.vault.azure.net/";
    var connectionStringSecret = "ana-db-connectionstring";
    var client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
    KeyVaultSecret secret = await client.GetSecretAsync(connectionStringSecret);
    connString = secret.Value;
}

Console.WriteLine($"MY: Connection string: { connString}");

var cosmosResource = localCosmosResource ?? cosmosDb;
if (cosmosResource == null)
{
    throw new InvalidOperationException("Cosmos DB resource is not configured. Please check your connection string or Azure Cosmos DB setup.");
}

var apiServiceBuilder = builder.AddProject<Projects.ana_ApiService>("apiservice")
    .WithReference(cosmosResource)
    .WithEnvironment("DefaultAdminPassword", defaultAdminPassword)
    .WithEnvironment("DefaultAdminPasswordIsEmpty", defaultAdminPasswordIsEmpty)
    .WithEnvironment("issuer-signing-key", builder.Configuration["issuer-signing-key"])
    .WaitFor(cosmosResource);

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



//var cosmosDbConnection = builder.ExecutionContext.IsPublishMode ?
//	builder.AddAzureCosmosDB("cosmosDb", (cosmos, construct, account, databases) =>
//	{
//		account.AssignProperty(x => x.ConsistencyPolicy.DefaultConsistencyLevel, $"'Session'");
//		account.AssignProperty(x => x.DatabaseAccountOfferType, $"'{CosmosDBAccountOfferType.Standard}'");
//		account.AssignProperty(x => x.Locations[0].LocationName, $"'{AzureLocation.UKWest.Name}'");

//		var capabilities = account.Properties.Capabilities ?? [];
//		capabilities.Add(new CosmosDBAccountCapability()
//		{
//			Name = "EnableServerless"
//		});

//		var capabilitiesString = string.Join(",", capabilities.Select(c => $"{{name: '{c.Name}'}}"));

//		account.AssignProperty(x => x.Capabilities, $"[{capabilitiesString}]");
//	})

//cosmosResource.AddCosmosDatabase("anaDb");

// var cosmos = builder.AddConnectionString("cosmos-db");

// builder.AddConnectionString("cosmosDb");
//#pragma warning restore AZPROVISION001


builder.Build().Run();
