using LabTrack.Application.Dtos.Usage;

namespace LabTrack.Application.Interfaces;

public interface IPcUsageService
{
    Task<PcUsageDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PcUsageDto?> GetActiveSessionByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default);

    Task<PcUsageDto?> GetActiveSessionByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PcUsageDto>> GetUserSessionHistoryAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PcUsageDto>> GetLaboratoryActiveSessionsAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default);

    Task<PcUsageDto> StartSessionAsync(
        StartSessionDto dto,
        CancellationToken cancellationToken = default);

    Task<PcUsageDto> EndSessionAsync(
        EndSessionDto dto,
        CancellationToken cancellationToken = default);

    Task ForceEndSessionAsync(
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default);

    Task SendHeartbeatAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}