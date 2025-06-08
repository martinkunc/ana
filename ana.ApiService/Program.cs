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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("cosmosdb");
    options.UseCosmos(connectionString, "IdentityDatabase");
});

// builder.Services.AddDefaultIdentity<IdentityUser>(options =>
// {
//     // Configure identity options
// })
// .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();


builder.Services.AddSingleton<CosmosClient>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("cosmosdb");
    return new CosmosClient(connectionString);
});

// Duplicate identity provider
// builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//     .AddUserStore<CosmosUserStore>()
//     .AddDefaultTokenProviders();



var identityServerBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    
    // Disable automatic redirects
    options.UserInteraction.ErrorUrl = "/error";
    options.UserInteraction.LoginUrl = null;
    options.UserInteraction.LoginReturnUrlParameter = null;
})
.AddInMemoryIdentityResources(Config.GetResources())
.AddInMemoryApiScopes(Config.GetApiScopes())
.AddInMemoryApiResources(Config.GetApis())
.AddInMemoryClients(Config.GetClients(builder.Configuration))
.AddAspNetIdentity<ApplicationUser>();

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

app.MapAnaApi();




app.Run();


