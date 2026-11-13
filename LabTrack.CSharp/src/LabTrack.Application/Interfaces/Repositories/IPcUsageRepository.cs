using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IPcUsageRepository : IGenericRepository<PcUsageLog>
{
    Task<PcUsageLog?> GetActiveSessionByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default);

    Task<PcUsageLog?> GetActiveSessionByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PcUsageLog>> GetActiveSessionsByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PcUsageLog>> GetSessionHistoryByStudentIdAsync(
        Guid studentId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PcUsageLog>> GetSessionsByDateRangeAsync(
        Guid laboratoryId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveSessionAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}