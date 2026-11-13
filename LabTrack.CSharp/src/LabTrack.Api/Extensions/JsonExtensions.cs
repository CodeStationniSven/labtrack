using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace LabTrack.Api.Extensions;

public static class JsonExtensions
{
    public static IMvcBuilder AddCrossDeviceJsonOptions(this IMvcBuilder builder)
    {
        return builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
    }
}
