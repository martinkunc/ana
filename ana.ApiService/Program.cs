using System.Security.Cryptography.X509Certificates;
using ana.ServiceDefaults;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ana.SharedNet;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Information);
}).CreateLogger<Program>();

logger.LogInformation($"Starting application with INFO: ");
logger.LogDebug($"Starting application with Debug: ");

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Configure detailed logging for development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
    builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Information);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Information);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Information);
}

var webAppUrl = Environment.GetEnvironmentVariable("WebApp__Url");
var reactAppUrl = Environment.GetEnvironmentVariable("ReactApp__Url");

string SecretConnectionString = await builder.GetFromSecretsOrVault(Config.SecretNames.AnaDbConnectionString);
var SecretFromEmail = await builder.GetFromSecretsOrVault(Config.SecretNames.FromEmail);
var SecretSendGridKey = await builder.GetFromSecretsOrVault(Config.SecretNames.SendGridKey);
var SecretTwilioAccountSID = await builder.GetFromSecretsOrVault(Config.SecretNames.TwilioAccountSid);
var SecretTwilioAccountToken = await builder.GetFromSecretsOrVault(Config.SecretNames.TwilioAccountToken);
var SecretWhatsAppFrom = await builder.GetFromSecretsOrVault(Config.SecretNames.WhatsAppFrom);
var SecretWebAppClientSecret = await builder.GetFromSecretsOrVault(Config.SecretNames.WebAppClientSecret);
var SecretBlazorClientSecret = await builder.GetFromSecretsOrVault(Config.SecretNames.BlazorClientSecret);

string env = builder.Environment.EnvironmentName;
Console.WriteLine($"Environment: {env}");

var envDnsSuffix = Environment.GetEnvironmentVariable("CONTAINER_APP_ENV_DNS_SUFFIX");
var serviceName = "apiservice"; // Your service name as defined in Container Apps
var isRunningOnAzureContainerApps = !string.IsNullOrEmpty(envDnsSuffix);
Console.WriteLine($"isRunningOnAzureContainerApps: {isRunningOnAzureContainerApps}");
var externalPublicDomain = "https://anniversarynotification.com";
var externalUrl = !isRunningOnAzureContainerApps ? builder.Configuration["ASPNETCORE_EXTERNAL_URL"] : externalPublicDomain;

// Currently on Azure it detects isRunningOnAzureContainerApps as true and uses externalPublicDomain,
// but that means it doesn't work directly in Azure Container Apps
Console.WriteLine($"MY: External URL: {externalUrl}");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowWeb", policy =>
        {
            policy.WithOrigins(webAppUrl, reactAppUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        
        options.Authority = externalUrl;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer =  externalUrl,
            ValidAudience = IdentityServerConfig.IdentityServer.AudienceName
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
            }
        };
    });



// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.AddAzureCosmosClient("cosmos-db");

foreach (var conf in builder.Configuration.AsEnumerable())
{

    logger.LogInformation($"Config: {conf.Key} = {conf.Value}");
}


builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var accountEndpoint = "";
    var accountKey = "";
    var parts = SecretConnectionString.Split(';');
    foreach (var part in parts)
    {
        if (part.StartsWith("AccountEndpoint=", StringComparison.OrdinalIgnoreCase))
            accountEndpoint = part.Substring("AccountEndpoint=".Length);
        else if (part.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
            accountKey = part.Substring("AccountKey=".Length);
    }

    // Configure Cosmos options explicitly
    options.UseCosmos(
        accountEndpoint: accountEndpoint,
        accountKey: accountKey,
        databaseName: Config.Database.Name,
        cosmosOptionsAction: cosmosOptions =>
        {
            cosmosOptions.HttpClientFactory(() =>
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
                };

                return new HttpClient(httpMessageHandler);
            });
            cosmosOptions.LimitToEndpoint(true);
            cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
        }
        );

});


builder.Services.AddSingleton<CosmosClient>(provider =>
{
    CosmosClientOptions clientOptions = new()
    {
        HttpClientFactory = () =>
        {
            HttpMessageHandler httpMessageHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
            };

            return new HttpClient(httpMessageHandler);
        },
        ConnectionMode = ConnectionMode.Gateway,
        LimitToEndpoint = true
    };
    return new CosmosClient(SecretConnectionString, clientOptions);
});


builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(

      options => options.SignIn.RequireConfirmedAccount = false
    )
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 0;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>,
    UserClaimsPrincipalFactory<IdentityUser, IdentityRole>>();

var apiUrl = externalUrl;
var webClientsUrls = new List<string>();
if (builder.Environment.IsDevelopment())
{
    webClientsUrls.Add(webAppUrl);
    webClientsUrls.Add(reactAppUrl);
}
var identityServerBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // Disable automatic redirects
    options.UserInteraction.ErrorUrl = "/error";
    options.UserInteraction.LoginUrl = "/account/login";
    options.UserInteraction.LoginReturnUrlParameter = "returnUrl";
    options.KeyManagement.Enabled = false;
    options.IssuerUri = externalUrl;
})
.AddInMemoryIdentityResources(IdentityServerConfig.GetResources())
.AddInMemoryApiScopes(IdentityServerConfig.GetApiScopes())
.AddInMemoryApiResources(IdentityServerConfig.GetApis())
.AddInMemoryClients(IdentityServerConfig.GetClients(builder.Configuration, apiUrl, webClientsUrls, SecretWebAppClientSecret))
//.AddApiAuthorization<IdentityUser, ApplicationDbContext>()
.AddAspNetIdentity<IdentityUser>();


// if (!builder.Environment.IsDevelopment())
// {
//     // Production: Use Azure Key Vault for data protection
// // Todo:
//     // builder.Services.AddDataProtection()
//     //     .PersistKeysToAzureBlobStorage(new Uri($"{builder.Configuration["DataProtection:BlobStorage:ContainerUri"]}/keys.xml"))
//     //     .ProtectKeysWithAzureKeyVault(new Uri($"{Config.KeyVault.KeyVaultUrl}/keys/dataprotection-key"), new DefaultAzureCredential())
//     //     .SetApplicationName("ana-identityserver");
// }
// else
// {
//     // Development: Use local file system
//     builder.Services.AddDataProtection()
//         .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "ana-dataprotection-keys")))
//         .SetApplicationName("ana-identityserver");
// }

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1); // Set to match or exceed your JWT lifetime
    options.SlidingExpiration = true; // Optional: extends session on activity
});

builder.Services.AddRazorPages();


var identityServerKeyPath = builder.Configuration["IdentityServerKeyPath"];
if (identityServerKeyPath != null)
{
    if (!File.Exists(identityServerKeyPath))
    {
        throw new FileNotFoundException("Identity server key file not found.", identityServerKeyPath);
    }
    var bytes = File.ReadAllBytes(identityServerKeyPath);
    var importedCertificate = X509CertificateLoader.LoadPkcs12(bytes, null);

    // Load the signing certificate from the specified path
    identityServerBuilder.AddSigningCredential(importedCertificate);
}
else
{
    var client = new SecretClient(new Uri(Config.KeyVault.KeyVaultUrl), new DefaultAzureCredential());
    KeyVaultSecret secret = await client.GetSecretAsync(IdentityServerConfig.IdentityServer.CertificateName);

    // The secret value should be the Base64 encoded PFX
    var pfxBytes = Convert.FromBase64String(secret.Value);
    //var cert = new X509Certificate2(pfxBytes);
    var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, null);

    if (cert == null)
    {
        throw new InvalidOperationException("Failed to load the signing certificate from Key Vault.");
    }

    identityServerBuilder.AddSigningCredential(cert);
}


builder.Services.AddAuthorization();


var httpClient = new HttpClient();

builder.Services.AddHttpClient(
    "Auth",
    opt => opt.BaseAddress = new Uri(externalUrl))
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiClient>>();
    var loggerFac = sp.GetRequiredService<ILogger<ApiHttpClientFactory>>();
    return new ApiClient(new ApiHttpClientFactory(httpClientFactory, externalUrl, SecretWebAppClientSecret, loggerFac), logger);
});


builder.Services.AddSingleton<DailyTaskService>();
var taskService = builder.Services.BuildServiceProvider().GetRequiredService<DailyTaskService>();
taskService.SetSecrets(SecretFromEmail, SecretSendGridKey, SecretTwilioAccountSID, SecretTwilioAccountToken, SecretWhatsAppFrom);

//await taskService.RunNowAsync();

builder.Services.AddSingleton<IApiEndpoints>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var logger = sp.GetRequiredService<ILogger<ApiEndpoints>>();
    var dbContextFactory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    return new ApiEndpoints(logger, dbContextFactory, httpContextAccessor, taskService);
});



var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseWebAssemblyDebugging();
    app.UseDeveloperExceptionPage();

    // Add detailed request logging for debugging
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("REQUEST: {Method} {Path} {QueryString}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

            await next();

            logger.LogInformation("RESPONSE: {Method} {Path} -> {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
    });
}
// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (builder.Environment.IsDevelopment())
{
    app.UseCors("AllowWeb");
}
app.UseIdentityServer();

app.UseRouting();

// Configure authentication to skip debugging endpoints
app.UseWhen(context => 
{
    var isDebugPath = context.Request.Path.StartsWithSegments("/_framework/debug") ||
                     context.Request.Path.StartsWithSegments("/_framework/debug/") ||
                     context.Request.Path.StartsWithSegments("/_framework/debug/ws-proxy");
    
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    if (isDebugPath)
    {
        logger.LogInformation("SKIPPING AUTH for debug path: {Path}", context.Request.Path);
    }
    
    return !isDebugPath;
}, 
appBuilder =>
{
    appBuilder.UseAuthentication();
    appBuilder.UseAuthorization();
});

app.UseFileServer();
app.UseBlazorFrameworkFiles();

app.MapRazorPages();
app.MapApiEndpoints();
app.MapFallbackToFile("index.html");



var builder1 = new DbContextOptionsBuilder<ApplicationDbContext>();

builder1.UseCosmos(SecretConnectionString, Config.Database.Name);

using (var dbContext = new ApplicationDbContext(builder1.Options))
{
    await SeedDatabase.Initialize(app.Services);
}

app.Run();

