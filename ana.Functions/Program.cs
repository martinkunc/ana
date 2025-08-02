using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ana.SharedNet;


var builder = FunctionsApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Program");

logger.LogInformation($"Starting application with INFO: ");
logger.LogDebug($"Starting application with Debug: ");


var externalUrl = builder.Configuration["ApiService:Url"] 
    ?? throw new InvalidOperationException("API URL not configured");

logger.LogInformation("API URL configured as: {ApiUrl}", externalUrl);

var SecretWebAppClientSecret = await builder.GetFromSecretsOrVault(Config.SecretNames.WebAppClientSecret);

Console.WriteLine($"MY: External URL: {externalUrl}");

builder.ConfigureFunctionsWebApplication();


builder.Services.AddHttpClient();

builder.Services.AddSingleton<IApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiClient>>();
    var loggerFac = sp.GetRequiredService<ILogger<FunctionsHttpClientFactory>>();
    return new ApiClient(new FunctionsHttpClientFactory(httpClientFactory, externalUrl, SecretWebAppClientSecret, loggerFac), logger);
});

builder.Build().Run();

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var loggerFactory2 = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
var logger2 = loggerFactory2.CreateLogger("Program");
logger2.LogInformation("Starting functions application with INFO: ");
Console.WriteLine("Starting functions application from console ");

var app = builder.Build();
app.Run();