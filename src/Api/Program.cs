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
await app.Services.InitializeInfrastructureAsync(app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
