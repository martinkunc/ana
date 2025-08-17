using ana.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using ana.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string env = builder.HostEnvironment.Environment;
Console.WriteLine($"Environment: {env}");

var baseAddress = builder.HostEnvironment.BaseAddress;
var apiServiceUrlConfig = builder.Configuration["ApiService__Url"];

var disableAuth = builder.Configuration.GetValue<bool>("DisableAuth");

var isLocalProdTesting = builder.HostEnvironment.IsProduction()
    && Uri.TryCreate(apiServiceUrlConfig, new UriCreationOptions { }, out var apiServiceParsedUrl)
    && Uri.TryCreate(baseAddress, new UriCreationOptions { }, out var baseAddressParsedUrl)
    && apiServiceParsedUrl.IsLoopback && baseAddressParsedUrl.IsLoopback;

var apiServiceUrl = baseAddress;
var authorityUrl = apiServiceUrl;
if (builder.HostEnvironment.IsDevelopment())
{
    apiServiceUrl = apiServiceUrlConfig;
    authorityUrl = apiServiceUrlConfig;
}


// prints configurations for debugging purposes
foreach (var conf in builder.Configuration.AsEnumerable())
{
    Console.WriteLine($"Config: {conf.Key} = {conf.Value}");
}
var baseAddressNoSlash = baseAddress.TrimEnd('/');
var postLogoutRedirectUri = apiServiceUrl;
if (isLocalProdTesting)
{
    Console.WriteLine("MY: ApiService__Url is loopback, using /api.");
    apiServiceUrl = baseAddressNoSlash + "/api";
    postLogoutRedirectUri = apiServiceUrlConfig;
    authorityUrl = apiServiceUrlConfig;
}

Console.WriteLine($"BaseAddress URL: {baseAddress}");
Console.WriteLine($"apiServiceUrl: {apiServiceUrl}");
Console.WriteLine($"authorityUrl: {authorityUrl}");
Console.WriteLine($"postLogoutRedirectUri: {postLogoutRedirectUri}");
if (apiServiceUrl == null)
{
    throw new InvalidOperationException("ApiService__Url configuration is missing. Please check your appsettings or environment variables.");
}


// Configure OIDC authentication
if (!disableAuth)
{
    builder.Services.AddOidcAuthentication(options =>
    {
        options.ProviderOptions.DefaultScopes.Add("ana_api");
        options.ProviderOptions.ClientId = "blazor"; // Client ID registered in IdentityServer
        options.ProviderOptions.Authority = authorityUrl;
        options.ProviderOptions.PostLogoutRedirectUri = $"{postLogoutRedirectUri}/account/login?returnUrl={baseAddressNoSlash}";
        options.ProviderOptions.RedirectUri = $"{baseAddress}authentication/login-callback";
        options.ProviderOptions.ResponseType = "code"; // Use authorization code flow
        options.AuthenticationPaths.LogOutPath = "authentication/logout";
        options.AuthenticationPaths.LogOutCallbackPath = "authentication/logout-callback";
        options.AuthenticationPaths.LogOutFailedPath = "authentication/logout-failed";
    });
}

builder.Services.AddTransient<CookieHandler>();

builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<UserDisplayNameService>();
builder.Services.AddScoped<UserSelectedGroupService>();

// Register the TokenService for automatic token refresh (overridden later if auth disabled)
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddHttpClient(
    "Auth",
    opt => opt.BaseAddress = new Uri(apiServiceUrl))
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped<IApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var tokenService = sp.GetRequiredService<ITokenService>();
    var logger = sp.GetRequiredService<ILogger<ApiClient>>();
    var loggerFac = sp.GetRequiredService<ILogger<WebHttpClientFactory>>();

    return new ApiClient(new WebHttpClientFactory(httpClientFactory, apiServiceUrl, tokenService, loggerFac), logger);
});

// Local production bypass: fabricate an authenticated user to access app without OIDC round-trips
if (disableAuth)
{
    Console.WriteLine("Auth disabled via configuration. Using bypass authentication.");
    builder.Services.AddSingleton(new LocalProdBypassFlag(true));
    builder.Services.AddScoped<AuthenticationStateProvider, BypassAuthenticationStateProvider>();
    builder.Services.AddScoped<ITokenService, DummyTokenService>();
}
else
{
    builder.Services.AddSingleton(new LocalProdBypassFlag(false));
}

await builder.Build().RunAsync();
