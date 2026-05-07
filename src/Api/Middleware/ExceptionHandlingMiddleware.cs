using System.Net;
using System.Text.Json;
using FluentValidation;

namespace SafetyScale.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
            context.Response.StatusCode = exception switch
            {
                ValidationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };
            context.Response.ContentType = "application/json";

            object payload = exception switch
            {
                ValidationException validationException => new
                {
                    title = "Validation Error",
                    status = context.Response.StatusCode,
                    detail = "One or more validation failures occurred.",
                    errors = validationException.Errors.Select(x => x.ErrorMessage)
                },
                _ => new
                {
                    title = "Internal Server Error",
                    status = context.Response.StatusCode,
                    detail = "An unexpected error occurred. Check logs for details."
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
