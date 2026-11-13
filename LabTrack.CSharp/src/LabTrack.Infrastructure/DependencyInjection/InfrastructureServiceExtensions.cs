using LabTrack.Infrastructure.Ai;
using LabTrack.Infrastructure.Audit;
using LabTrack.Infrastructure.Authentication;
using LabTrack.Infrastructure.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabTrack.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        services.AddHttpContextAccessor();

        
        services.Configure<PythonAiClientOptions>(configuration.GetSection(PythonAiClientOptions.SectionName));
        services.AddHttpClient<PythonAiClient>();
        services.AddScoped<AiContextBuilder>();
        services.AddScoped<AiResponseValidator>();

        
        services.AddScoped<IAuditContextAccessor, AuditContextAccessor>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        
        services.AddCustomAuthentication(configuration);
        services.AddScoped<LabTrack.Application.Interfaces.IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Authorization Infrastructure
        services.AddCustomAuthorization();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        return services;
    }
}