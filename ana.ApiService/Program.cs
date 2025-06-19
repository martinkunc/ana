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


var builder = WebApplication.CreateBuilder(args);

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
    config.AddDebug();
}).CreateLogger<Program>();

logger.LogInformation($"Starting application with INFO: ");
logger.LogDebug($"Starting application with Debug: ");
logger.LogError($"Starting application with Error: ");

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();



//builder.AddDefaultAuthentication();

// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultAuthenticateScheme = "Bearer";
//     options.DefaultChallengeScheme = "Bearer";
//     options.DefaultScheme = "Bearer";
// })
//     .AddJwtBearer("Bearer", options =>
//     {
//         options.Authority = builder.Configuration["Identity:Url"] ?? "https://localhost:5001"; // Set to your IdentityServer URL
//         options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//         {
//             ValidateAudience = false // Or set to true and configure Audience as needed
//         };
//         options.RequireHttpsMetadata = false; // Set to true in production
//         options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
//         {
//             OnChallenge = context =>
//             {
//                 context.HandleResponse();
//                 context.Response.StatusCode = 401;
//                 context.Response.ContentType = "application/json";
//                 return context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
//             },
//             OnForbidden = context =>
//             {
//                 context.Response.StatusCode = 403;
//                 context.Response.ContentType = "application/json";
//                 return context.Response.WriteAsync("{\"error\": \"Forbidden\"}");
//             }
//         };
//     });

var issuerSigningKey = Convert.FromBase64String(await builder.GetFromSecretsOrVault(Config.SecretsKeyNames.IssuerSigningKeySecretName, Config.KeyVault.IssuerSigningKeySecretName));

builder.Services.AddAuthentication(options =>
{ 
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        
        options.Authority = builder.Configuration["Identity:Url"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(issuerSigningKey),
            ValidIssuer =  Config.IdentityServer.IssuerName,
            ValidAudience = Config.IdentityServer.AudienceName
        };
        options.Events = new JwtBearerEvents
        {
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


string connectionString = await builder.GetFromSecretsOrVault(Config.SecretsKeyNames.ConnectionStringCosmos, Config.KeyVault.ConnectionStringSecretName);

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
// {
//     var accountEndpoint = "";
//     var accountKey = "";
//     var parts = connectionString.Split(';');
//     foreach (var part in parts)
//     {
//         if (part.StartsWith("AccountEndpoint=", StringComparison.OrdinalIgnoreCase))
//             accountEndpoint = part.Substring("AccountEndpoint=".Length);
//         else if (part.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
//             accountKey = part.Substring("AccountKey=".Length);
//     }

//     // Configure Cosmos options explicitly
//     options.UseCosmos(
//         accountEndpoint: accountEndpoint,
//         accountKey: accountKey,
//         databaseName: Config.Database.Name,
//         cosmosOptionsAction: cosmosOptions =>
//         {
//             cosmosOptions.HttpClientFactory(() =>
//             {
//                 HttpMessageHandler httpMessageHandler = new HttpClientHandler()
//                 {
//                     ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
//                 };

//                 return new HttpClient(httpMessageHandler);
//             });
//             cosmosOptions.LimitToEndpoint(true);
//             cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
//         }
//         );

// });

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var accountEndpoint = "";
    var accountKey = "";
    var parts = connectionString.Split(';');
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



// builder.Services.AddDefaultIdentity<IdentityUser>(options =>
// {
//     // Configure identity options
// })
// .AddEntityFrameworkStores<ApplicationDbContext>();

// builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//         .AddEntityFrameworkStores<ApplicationDbContext>()
//         .AddDefaultTokenProviders();




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
    return new CosmosClient(connectionString, clientOptions);
});


builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(

      options => options.SignIn.RequireConfirmedAccount = false
    )
    //.AddDefaultUI() // Use this if Identity Scaffolding is in use
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

var envDnsSuffix = Environment.GetEnvironmentVariable("CONTAINER_APP_ENV_DNS_SUFFIX");
var serviceName = "apiservice"; // Your service name as defined in Container Apps


var externalUrl = builder.Configuration["ASPNETCORE_EXTERNAL_URL"] ?? $"https://{serviceName}.{envDnsSuffix}";

Console.WriteLine($"MY: External URL: {externalUrl}");

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

})
.AddInMemoryIdentityResources(Config.GetResources())
.AddInMemoryApiScopes(Config.GetApiScopes())
.AddInMemoryApiResources(Config.GetApis())
.AddInMemoryClients(Config.GetClients(builder.Configuration, externalUrl))
//.AddApiAuthorization<IdentityUser, ApplicationDbContext>()
.AddAspNetIdentity<IdentityUser>();

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
    KeyVaultSecret secret = await client.GetSecretAsync(Config.IdentityServer.CertificateName);

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

var tokenService = new TokenService(
    builder.Configuration,
    LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<TokenService>(),
    issuerSigningKey
);

builder.Services.AddSingleton<ITokenService>(tokenService);

var httpClient =  new HttpClient();

var apiClient = new ApiClient(httpClient, externalUrl, tokenService,
    LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<ApiClient>());

builder.Services.AddSingleton<IApiClient>(apiClient);

builder.Services.AddSingleton<IApiEndpoints, ApiEndpoints>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();
app.UseFileServer();
app.UseBlazorFrameworkFiles();
app.MapRazorPages();

app.MapFallbackToFile("index.html");

app.MapApiEndpoints();


var builder1 = new DbContextOptionsBuilder<ApplicationDbContext>();

builder1.UseCosmos(connectionString, Config.Database.Name);

using (var dbContext = new ApplicationDbContext(builder1.Options))
{
    await SeedDatabase.Initialize(app.Services);


}


app.Run();

