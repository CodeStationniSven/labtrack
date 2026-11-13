using Microsoft.AspNetCore.Hosting;

namespace LabTrack.Api.Extensions;

public static class KestrelExtensions
{
    public static IWebHostBuilder ConfigureCrossDeviceKestrel(this IWebHostBuilder webHost)
    {
        return webHost.ConfigureKestrel(options =>
        {
            // Bind to 0.0.0.0 to accept traffic from iOS, Android, Desktop & LAN devices
            options.ListenAnyIP(5000);
        });
    }
}
