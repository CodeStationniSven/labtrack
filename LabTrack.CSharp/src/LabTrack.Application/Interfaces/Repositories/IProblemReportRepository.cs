using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IProblemReportRepository : IGenericRepository<ProblemReport>
{
    Task<IReadOnlyList<ProblemReport>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReport>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReport>> GetByReporterIdAsync(
        Guid reporterId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReport>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReport>> GetBySeverityAsync(
        string severity, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProblemReport>> GetUnresolvedReportsAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default);
}