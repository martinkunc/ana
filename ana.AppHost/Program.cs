var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ana_ApiService>("apiservice")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.ana_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);


builder.AddNpmApp("reactapp", "../ana.react")
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

//builder.AddNpmApp("reactvite", "../AspireJavaScript.Vite")
//    .WithReference(weatherApi)
//    .WithEnvironment("BROWSER", "none")
//    .WithHttpEndpoint(env: "VITE_PORT")
//    .WithExternalHttpEndpoints()
//    .PublishAsDockerFile();

builder.Build().Run();
