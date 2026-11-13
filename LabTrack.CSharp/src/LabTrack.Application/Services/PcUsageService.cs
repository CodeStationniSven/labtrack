using LabTrack.Application.Dtos.Usage;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class PcUsageService : IPcUsageService
{
    private readonly IUnitOfWork _unitOfWork;

    public PcUsageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PcUsageDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(id));

        return await Task.FromResult<PcUsageDto?>(null);
    }

    public async Task<PcUsageDto?> GetActiveSessionByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default)
    {
        if (workstationId == Guid.Empty)
            throw new ArgumentException("Workstation ID cannot be empty.", nameof(workstationId));

        return await Task.FromResult<PcUsageDto?>(null);
    }

    public async Task<PcUsageDto?> GetActiveSessionByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return await Task.FromResult<PcUsageDto?>(null);
    }

    public async Task<IReadOnlyList<PcUsageDto>> GetUserSessionHistoryAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return await Task.FromResult<IReadOnlyList<PcUsageDto>>(Array.Empty<PcUsageDto>());
    }

    public async Task<IReadOnlyList<PcUsageDto>> GetLaboratoryActiveSessionsAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default)
    {
        if (laboratoryId == Guid.Empty)
            throw new ArgumentException("Laboratory ID cannot be empty.", nameof(laboratoryId));

        return await Task.FromResult<IReadOnlyList<PcUsageDto>>(Array.Empty<PcUsageDto>());
    }

    public async Task<PcUsageDto> StartSessionAsync(
        StartSessionDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.WorkstationId == Guid.Empty || dto.StudentId == Guid.Empty)
            throw new ArgumentException("Workstation ID and Student ID must be valid.");

        return await Task.FromResult(new PcUsageDto
        {
            SessionId = Guid.NewGuid(),
            StudentId = dto.StudentId,
            WorkstationId = dto.WorkstationId,
            LaboratoryId = dto.LaboratoryId,
            StartedAtUtc = DateTime.UtcNow,
            SystemStatus = "Active"
        });
    }

    public async Task<PcUsageDto> EndSessionAsync(
        EndSessionDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.SessionId == Guid.Empty)
            throw new ArgumentException("Session ID must be provided.", nameof(dto.SessionId));

        if (dto.WorkstationId == Guid.Empty || dto.StudentId == Guid.Empty)
            throw new ArgumentException("Workstation ID and Student ID must be valid.");

        return await Task.FromResult(new PcUsageDto
        {
            SessionId = dto.SessionId,
            WorkstationId = dto.WorkstationId,
            StudentId = dto.StudentId,
            EndedAtUtc = dto.EndedAtUtc,
            WorkstationCondition = dto.WorkstationCondition,
            SystemStatus = "Ended"
        });
    }

    public async Task ForceEndSessionAsync(
        Guid sessionId, 
        string reason, 
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason for ending session must be provided.", nameof(reason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SendHeartbeatAsync(
        Guid sessionId, 
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}