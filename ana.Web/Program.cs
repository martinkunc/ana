using ana.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;
using ana.Web;
using ana.Web.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };


var baseAddress = builder.HostEnvironment.BaseAddress;
Console.WriteLine($"BaseAddress URL: {baseAddress}");

// "PostLogoutRedirectUri": "https://localhost:5001/authentication/logout-callback",
// "RedirectUri": "https://localhost:5001/authentication/login-callback",

// Configure OIDC authentication
builder.Services.AddOidcAuthentication(options =>
{
    //builder.Configuration.Bind("Oidc", options.ProviderOptions);
    //options.ProviderOptions.DefaultScopes.Add("ana");
    options.ProviderOptions.DefaultScopes.Add("ana_api");
//options.ProviderOptions.DefaultScopes.Add("ana api"); // Add custom API scope
    options.ProviderOptions.ClientId = "blazor"; // Client ID registered in IdentityServer
    options.ProviderOptions.Authority = baseAddress ;
    options.ProviderOptions.PostLogoutRedirectUri = $"{baseAddress}authentication/login";
    options.ProviderOptions.RedirectUri = $"{baseAddress}authentication/login-callback";
    options.ProviderOptions.ResponseType = "code"; // Use authorization code flow
});

// register the cookie handler
builder.Services.AddTransient<CookieHandler>();

// set up authorization
builder.Services.AddAuthorizationCore();

builder.Services.AddSingleton<UserDisplayNameService>();

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// builder.Services.AddScoped(sp => sp.GetRequiredService<IAccessTokenProvider>()
//     .CreateHttpClient(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }));


//var issuerSigningKey = Convert.FromBase64String(await builder.GetFromSecretsOrVault(Config.SecretsKeyNames.IssuerSigningKeySecretName, Config.KeyVault.IssuerSigningKeySecretName));

// var tokenService = new TokenService(
//     builder.Configuration,
//     LoggerFactory.Create(builder => builder.AddConsole())
//         .CreateLogger<TokenService>(),
//     issuerSigningKey
// );

// builder.Services.AddSingleton<ITokenService>(tokenService);

// var httpClient =  new HttpClient();

// var apiClient = new ApiClient(httpClient, baseAddress, tokenService,
//     LoggerFactory.Create(builder => builder.AddConsole())
//         .CreateLogger<ApiClient>());

// builder.Services.AddSingleton<IApiClient>(apiClient);

builder.Services.AddHttpClient(
    "Auth",
    opt => opt.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddSingleton<IApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var accessTokenProvider = sp.GetRequiredService<IAccessTokenProvider>();
    var authenticationStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    var logger = sp.GetRequiredService<ILogger<ApiClient>>();
    var loggerFac = sp.GetRequiredService<ILogger<WebHttpClientFactory>>();

    return new ApiClient(new WebHttpClientFactory(httpClientFactory, baseAddress, accessTokenProvider, loggerFac), logger);
});

await builder.Build().RunAsync();

