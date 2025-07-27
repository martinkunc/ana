using ana.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string env = builder.HostEnvironment.Environment;
Console.WriteLine($"Environment: {env}");

var baseAddress = builder.HostEnvironment.BaseAddress;
var apiServiceUrlConfig = builder.Configuration["ApiService__Url"];


var apiServiceUrl = baseAddress;
if (builder.HostEnvironment.IsDevelopment())
{
    apiServiceUrl = apiServiceUrlConfig;
}
Console.WriteLine($"BaseAddress URL: {baseAddress}");
Console.WriteLine($"apiServiceUrl: {apiServiceUrl}");



// Configure OIDC authentication
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.DefaultScopes.Add("ana_api");
    options.ProviderOptions.ClientId = "blazor"; // Client ID registered in IdentityServer
    options.ProviderOptions.Authority = apiServiceUrl;
    options.ProviderOptions.PostLogoutRedirectUri = $"{baseAddress}authentication/login";
    options.ProviderOptions.RedirectUri = $"{baseAddress}authentication/login-callback";
    options.ProviderOptions.ResponseType = "code"; // Use authorization code flow
});

// register the cookie handler
builder.Services.AddTransient<CookieHandler>();

// set up authorization
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<UserDisplayNameService>();
builder.Services.AddScoped<UserSelectedGroupService>();

// Register the TokenService for automatic token refresh
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

await builder.Build().RunAsync();

