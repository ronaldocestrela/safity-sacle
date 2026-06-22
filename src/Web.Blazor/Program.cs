using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SafetyScale.Web.Blazor;
using SafetyScale.Web.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<AppConfiguration>();
builder.Services.AddSingleton<ApiUrlBuilder>();
builder.Services.AddScoped<BrowserSessionStorage>();
builder.Services.AddScoped(_ => new HttpClient());

await builder.Build().RunAsync();
