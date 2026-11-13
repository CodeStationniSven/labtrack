using LabTrack.Application.Dtos.Analytics;

namespace LabTrack.Application.Interfaces;

public interface IAnalyticsService
{
    Task<LaboratoryAnalyticsDto> GetLaboratoryUtilizationStatsAsync(
        Guid laboratoryId, 
        DateTime startUtc, 
        DateTime endUtc, 
        CancellationToken cancellationToken = default);

    Task<LaboratoryAnalyticsDto> GetPeakUsageHoursAsync(
        Guid laboratoryId, 
        DateTime startUtc, 
        DateTime endUtc, 
        CancellationToken cancellationToken = default);

    Task<AssetAnalyticsDto> GetEquipmentFailureRatesAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default);

    Task<FinancialAnalyticsDto> GetMaintenanceCostSummaryAsync(
        Guid? laboratoryId = null, 
        DateTime? startUtc = null, 
        DateTime? endUtc = null, 
        CancellationToken cancellationToken = default);
}