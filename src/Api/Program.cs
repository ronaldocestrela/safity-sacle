using Microsoft.AspNetCore.HttpOverrides;
using SafetyScale.Api.Extensions;
using SafetyScale.Api.Middleware;
using SafetyScale.Application;
using SafetyScale.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services
    .AddApiLayer(builder.Configuration)
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration);

var useForwardedHeaders = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Rede Compose / proxies internos apenas (nunca exponha a API direta à Internet com esta config).
#pragma warning disable ASPDEPR005
        options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();
var corsOrigins =
    builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? Array.Empty<string>();

await app.Services.InitializeInfrastructureAsync(app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
if (corsOrigins.Length > 0)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseMiddleware<TenantClaimMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
