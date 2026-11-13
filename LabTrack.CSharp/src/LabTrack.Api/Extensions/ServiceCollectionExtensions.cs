using LabTrack.Application.Interfaces;
using LabTrack.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LabTrack.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICommunicationService, CommunicationService>();
        services.AddScoped<IFinancialService, FinancialService>();
        // services.AddScoped<ILaboratoryService, LaboratoryService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<INetworkService, NetworkService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPcUsageService, PcUsageService>();
        services.AddScoped<IProblemReportService, ProblemReportService>();
        services.AddScoped<IQrService, QrService>();
        services.AddScoped<IRepairTicketService, RepairTicketService>();
        services.AddScoped<IReplacementService, ReplacementService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}



