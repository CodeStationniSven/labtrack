using LabTrack.Application.Dtos.Reports;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class ProblemReportService : IProblemReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProblemReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemReportDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Problem report ID cannot be empty.", nameof(id));

        return await Task.FromResult<ProblemReportDto?>(null);
    }

    public async Task<IReadOnlyList<ProblemReportDto>> GetByReporterIdAsync(
        Guid reporterId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (reporterId == Guid.Empty)
            throw new ArgumentException("Reporter ID cannot be empty.", nameof(reporterId));

        return await Task.FromResult<IReadOnlyList<ProblemReportDto>>(Array.Empty<ProblemReportDto>());
    }

    public async Task<IReadOnlyList<ProblemReportDto>> GetByAssetIdAsync(
        Guid assetId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset ID cannot be empty.", nameof(assetId));

        return await Task.FromResult<IReadOnlyList<ProblemReportDto>>(Array.Empty<ProblemReportDto>());
    }

    public async Task<IReadOnlyList<ProblemReportDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (laboratoryId == Guid.Empty)
            throw new ArgumentException("Laboratory ID cannot be empty.", nameof(laboratoryId));

        return await Task.FromResult<IReadOnlyList<ProblemReportDto>>(Array.Empty<ProblemReportDto>());
    }

    public async Task<IReadOnlyList<ProblemReportDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty.", nameof(status));

        return await Task.FromResult<IReadOnlyList<ProblemReportDto>>(Array.Empty<ProblemReportDto>());
    }

    public async Task<ProblemReportDto> CreateAsync(
        CreateProblemReportDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.ReporterId == Guid.Empty)
            throw new ArgumentException("Reporter ID must be provided.", nameof(dto.ReporterId));

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title cannot be empty.", nameof(dto.Title));

        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new ArgumentException("Description cannot be empty.", nameof(dto.Description));

        return await Task.FromResult(new ProblemReportDto
        {
            Id = Guid.NewGuid(),
            ReporterId = dto.ReporterId,
            AssetId = dto.AssetId,
            WorkstationId = dto.WorkstationId,
            LaboratoryId = dto.LaboratoryId,
            Title = dto.Title,
            Description = dto.Description,
            Severity = dto.Severity,
            Status = "Submitted",
            ReportedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<ProblemReportDto> UpdateStatusAsync(
        Guid id, 
        UpdateProblemReportStatusDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Problem report ID cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Status))
            throw new ArgumentException("Status cannot be empty.", nameof(dto.Status));

        return await Task.FromResult(new ProblemReportDto
        {
            Id = id,
            Status = dto.Status,
            ResolutionNotes = dto.ResolutionNotes
        });
    }

    public async Task AssignTechnicianAsync(
        Guid reportId, 
        Guid technicianId, 
        CancellationToken cancellationToken = default)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report ID cannot be empty.", nameof(reportId));

        if (technicianId == Guid.Empty)
            throw new ArgumentException("Technician ID cannot be empty.", nameof(technicianId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveReportAsync(
        Guid reportId, 
        string resolutionNotes, 
        CancellationToken cancellationToken = default)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report ID cannot be empty.", nameof(reportId));

        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new ArgumentException("Resolution notes must be provided.", nameof(resolutionNotes));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid reportId, 
        CancellationToken cancellationToken = default)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report ID cannot be empty.", nameof(reportId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
}