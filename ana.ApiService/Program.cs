using ana.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Url"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
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

foreach (var conf in builder.Configuration.AsEnumerable())
{
    Console.WriteLine($"Config: {conf.Key} = {conf.Value}");
}

var connectionString = builder.Configuration.GetConnectionString("IdentityDatabase");


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // var databaseName = builder.Configuration["CosmosDb:Database"] ?? "IdentityDatabase";
    // options.UseCosmos(connectionString, databaseName);
    var databaseName = builder.Configuration["CosmosDb:Database"] ?? "IdentityDatabase";
    //options.UseCosmos(connectionString, databaseName);

    // Parse the connection string manually
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
        databaseName: databaseName,
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

 

    // var connectionString = builder.Configuration.GetConnectionString("cosmosdb");
    // options.UseCosmos(connectionString, "IdentityDatabase");
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

// Duplicate identity provider
// builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//     .AddUserStore<CosmosUserStore>()
//     .AddDefaultTokenProviders();


builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
      options => options.SignIn.RequireConfirmedAccount = true // Always a good idea :)
    )
    //.AddDefaultUI() // Use this if Identity Scaffolding is in use
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, 
    UserClaimsPrincipalFactory<IdentityUser, IdentityRole>>();

var externalUrl = builder.Configuration["ASPNETCORE_EXTERNAL_URL"];

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
})
.AddInMemoryIdentityResources(Config.GetResources())
.AddInMemoryApiScopes(Config.GetApiScopes())
.AddInMemoryApiResources(Config.GetApis())
.AddInMemoryClients(Config.GetClients(builder.Configuration))
//.AddApiAuthorization<IdentityUser, ApplicationDbContext>()
.AddAspNetIdentity<IdentityUser>();

builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
    // TODO: Not recommended for production - you need to store your key material somewhere secure
    identityServerBuilder.AddDeveloperSigningCredential();
}
else
{
    identityServerBuilder.AddDeveloperSigningCredential(persistKey: false);
}

builder.Services.AddAuthorization();

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

app.MapAnaApi();


var setupCosmosDb = builder.Configuration["SetupCosmosDb"] ?? "false";
var cosmosIdentityDbName = "IdentityDatabase";

if (bool.TryParse(setupCosmosDb, out var setup) && setup)
{
    var builder1 = new DbContextOptionsBuilder<ApplicationDbContext>();
    builder1.UseCosmos(connectionString, cosmosIdentityDbName);

    using (var dbContext = new ApplicationDbContext(builder1.Options))
    {
        await SeedDatabase.Initialize(app.Services);

        
    }
}


app.Run();
