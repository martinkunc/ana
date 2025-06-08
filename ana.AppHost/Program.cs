using Aspire.Hosting;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Microsoft.Extensions.Hosting;



var builder = DistributedApplication.CreateBuilder(args);
IResourceBuilder<AzureCosmosDBResource> cosmosResource = null;
IResourceBuilder<Aspire.Hosting.Azure.AzureCosmosDBDatabaseResource> cosmosDb;
if (builder.Environment.IsDevelopment())
{
#pragma warning disable ASPIRECOSMOSDB001
    // In development, we can use a local Cosmos DB emulator
    cosmosResource = builder.AddAzureCosmosDB("cosmos-db")
        .RunAsEmulator(
                     emulator =>
                     {
                         //emulator.WithDataExplorer();
                         emulator.WithGatewayPort(8081);
                                                  
                     });
#pragma warning restore ASPIRECOSMOSDB001
    cosmosDb = cosmosResource.AddCosmosDatabase("IdentityDatabase");
}
else
{
    // In production, we use the actual Azure Cosmos DB
    cosmosResource = builder.AddAzureCosmosDB("cosmos-db").ConfigureInfrastructure((infra) =>
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
    cosmosDb = cosmosResource.AddCosmosDatabase("IdentityDatabase");
}

var apiService = builder.AddProject<Projects.ana_ApiService>("apiservice")
    .WithReference(cosmosDb)
    .WithEnvironment("CosmosDb__Database", "IdentityDatabase")
    .WaitFor(cosmosDb)
    .WithExternalHttpEndpoints();

// builder.AddProject<Projects.ana_Web>("webfrontend")
//     .WithExternalHttpEndpoints()
//     .WithReference(apiService)
//     .WaitFor(apiService);


var apiUrlHttp = apiService.GetEndpoint("http"); // or "https" if using HTTPS
var apiUrlHttps = apiService.GetEndpoint("https");

apiService.WithEnvironment("ASPNETCORE_EXTERNAL_URL", apiUrlHttps);

builder.AddNpmApp("reactapp", "../ana.react")
    .WithReference(apiService)
    .WaitFor(apiService)
    //.WithEnvironment("VITE_API_URL", apiService.Resource.GetEndpoints() GetHttpEndpointUrl())
    //.WithEnvironmentPrefix("VITE_")
    .WithEnvironment("services__apiservice__http__0", apiUrlHttp)
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

//builder.AddNpmApp("reactvite", "../AspireJavaScript.Vite")
//    .WithReference(weatherApi)
//    .WithEnvironment("BROWSER", "none")
//    .WithHttpEndpoint(env: "VITE_PORT")
//    .WithExternalHttpEndpoints()
//    .PublishAsDockerFile();

builder.Build().Run();
