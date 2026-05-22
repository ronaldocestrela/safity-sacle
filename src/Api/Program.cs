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
