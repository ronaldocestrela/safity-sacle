using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using SafetyScale.Web.Blazor;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton(AppJsonSerializerOptions.Create());
builder.Services.AddSingleton<AppConfiguration>();
builder.Services.AddSingleton<ApiUrlBuilder>();
builder.Services.AddScoped<BrowserSessionStorage>();
builder.Services.AddScoped<PlatformBrowserSessionStorage>();
builder.Services.AddScoped<JwtSessionStorage>();
builder.Services.AddScoped<PlatformJwtSessionStorage>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthSessionService>();
builder.Services.AddScoped<PlatformAuthSessionService>();
builder.Services.AddScoped<TenantsRegistrationClient>();
builder.Services.AddScoped<PlatformTenantsApiClient>();
builder.Services.AddScoped<PlatformPlansApiClient>();
builder.Services.AddScoped<SecurityGuardsApiClient>();
builder.Services.AddScoped<SchedulesApiClient>();
builder.Services.AddScoped<SectorsApiClient>();
builder.Services.AddScoped<UnavailableDaysApiClient>();

builder.Services.AddScoped<ApiHttpClient>(sp =>
{
    var urlBuilder = sp.GetRequiredService<ApiUrlBuilder>();
    var jsonOptions = sp.GetRequiredService<JsonSerializerOptions>();
    var sessionStorage = sp.GetRequiredService<JwtSessionStorage>();
    var platformSessionStorage = sp.GetRequiredService<PlatformJwtSessionStorage>();
    var authStateProvider = sp.GetRequiredService<CustomAuthStateProvider>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();

    var handlerPipeline = new UnauthorizedRedirectHandler(
            sessionStorage,
            platformSessionStorage,
            authStateProvider,
            navigationManager)
    {
        InnerHandler = new BearerTokenHandler(sessionStorage, platformSessionStorage)
        {
            InnerHandler = new HttpClientHandler(),
        },
    };

    var httpClient = new HttpClient(handlerPipeline)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    };

    return new ApiHttpClient(httpClient, urlBuilder, jsonOptions);
});

await builder.Build().RunAsync();
