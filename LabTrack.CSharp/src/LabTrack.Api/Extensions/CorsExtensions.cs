using Microsoft.Extensions.DependencyInjection;

namespace LabTrack.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCrossDeviceCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllDevices", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
}
