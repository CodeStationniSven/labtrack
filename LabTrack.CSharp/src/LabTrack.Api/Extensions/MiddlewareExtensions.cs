using System.Net;
using System.Text.Json;
using LabTrack.Api.Contracts;

namespace LabTrack.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<IApplicationBuilder>>();
                logger.LogError(ex, "Unhandled exception occurred during request processing.");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = ApiResponse<object>.Fail(
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred while processing your request.");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
            }
        });
    }
}