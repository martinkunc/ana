using ana.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;
using ana.Web;
using ana.Web.Pages;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };



Console.WriteLine($"BaseAddress URL: {builder.HostEnvironment.BaseAddress}");

// "PostLogoutRedirectUri": "https://localhost:5001/authentication/logout-callback",
// "RedirectUri": "https://localhost:5001/authentication/login-callback",

// Configure OIDC authentication
builder.Services.AddOidcAuthentication(options =>
{
    //builder.Configuration.Bind("Oidc", options.ProviderOptions);
    options.ProviderOptions.DefaultScopes.Add("ana"); // Add custom API scope
    options.ProviderOptions.ClientId = "blazor"; // Client ID registered in IdentityServer
    options.ProviderOptions.Authority = builder.HostEnvironment.BaseAddress;
    options.ProviderOptions.PostLogoutRedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login";
    options.ProviderOptions.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
    options.ProviderOptions.ResponseType = "code"; // Use authorization code flow
});

// register the cookie handler
builder.Services.AddTransient<CookieHandler>();

// set up authorization
builder.Services.AddAuthorizationCore();

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// builder.Services.AddScoped(sp => sp.GetRequiredService<IAccessTokenProvider>()
//     .CreateHttpClient(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }));

await builder.Build().RunAsync();

