using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IRepairTicketRepository : IGenericRepository<RepairTicket>
{
    Task<RepairTicket?> GetByTicketNumberAsync(
        string ticketNumber, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicket>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicket>> GetByAssetIdAsync(
        Guid assetId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicket>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicket>> GetByAssignedTechnicianIdAsync(
        Guid technicianId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicket>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicket>> GetByProblemReportIdAsync(
        Guid problemReportId, 
        CancellationToken cancellationToken = default);

    Task<bool> IsTicketNumberUniqueAsync(
        string ticketNumber, 
        Guid? excludeTicketId = null, 
        CancellationToken cancellationToken = default);
}