using LabTrack.Application.Dtos.Tickets;

namespace LabTrack.Application.Interfaces;

public interface IRepairTicketService
{
    Task<RepairTicketDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<RepairTicketDto?> GetByTicketNumberAsync(
        string ticketNumber, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicketDto>> GetByAssetIdAsync(
        Guid assetId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicketDto>> GetByTechnicianIdAsync(
        Guid technicianId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepairTicketDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<RepairTicketDto> CreateAsync(
        CreateRepairTicketDto dto, 
        CancellationToken cancellationToken = default);

    Task<RepairTicketDto> UpdateStatusAsync(
        Guid id, 
        UpdateRepairTicketStatusDto dto, 
        CancellationToken cancellationToken = default);

    Task AssignTechnicianAsync(
        Guid id, 
        Guid technicianId, 
        CancellationToken cancellationToken = default);

    Task CompleteRepairAsync(
        Guid id, 
        string repairSummary, 
        decimal laborCost, 
        decimal partsCost, 
        CancellationToken cancellationToken = default);

    Task CancelTicketAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default);
}