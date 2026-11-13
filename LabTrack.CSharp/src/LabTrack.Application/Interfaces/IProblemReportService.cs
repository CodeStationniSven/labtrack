using LabTrack.Application.Dtos.Reports;

namespace LabTrack.Application.Interfaces;

public interface IProblemReportService
{
    Task<ProblemReportDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReportDto>> GetByReporterIdAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReportDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReportDto>> GetByAssetIdAsync(
        Guid assetId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReportDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<ProblemReportDto> CreateAsync(
        CreateProblemReportDto dto, 
        CancellationToken cancellationToken = default);

    Task<ProblemReportDto> UpdateStatusAsync(
        Guid id, 
        UpdateProblemReportStatusDto dto, 
        CancellationToken cancellationToken = default);

    Task AssignTechnicianAsync(
        Guid id, 
        Guid technicianId, 
        CancellationToken cancellationToken = default);

    Task ResolveReportAsync(
        Guid id, 
        string resolutionNotes, 
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

}