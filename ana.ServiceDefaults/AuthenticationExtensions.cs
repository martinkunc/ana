using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Http;
namespace ana.ServiceDefaults;


public static class AuthenticationExtensions
{
    public static IServiceCollection AddDefaultAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // {
        //   "Identity": {
        //     "Url": "http://identity",
        //     "Audience": "basket"
        //    }
        // }

        var identitySection = configuration.GetSection("Identity");

        if (!identitySection.Exists())
        {
            // No identity section, so no authentication
            return services;
        }

        // prevent from mapping "sub" claim to nameidentifier.
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        services.AddAuthentication().AddJwtBearer(options =>
        {
            var identityUrl = identitySection.GetRequiredValue("Url");
            var audience = identitySection.GetRequiredValue("Audience");

            options.Authority = identityUrl;
            options.RequireHttpsMetadata = false;
            options.Audience = audience;

            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    // Skip the default logic.
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\": \"Forbidden\"}");
                }
            };
            
#if DEBUG
            //Needed if using Android Emulator Locally. See https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-8.0#android
            options.TokenValidationParameters.ValidIssuers = [identityUrl, "https://10.0.2.2:5243"];
#else
            options.TokenValidationParameters.ValidIssuers = [identityUrl];
#endif
            
            options.TokenValidationParameters.ValidateAudience = false;
        });

        services.AddAuthorization();

        return services;
    }
}
