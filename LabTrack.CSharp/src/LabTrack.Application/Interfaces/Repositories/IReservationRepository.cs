using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<IReadOnlyList<Reservation>> GetByUserIdAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetReservationsForTimeSlotAsync(
        Guid laboratoryId, 
        Guid? workstationId, 
        DateTime startUtc, 
        DateTime endUtc, 
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingReservationAsync(
        Guid laboratoryId, 
        Guid? workstationId, 
        DateTime startUtc, 
        DateTime endUtc, 
        Guid? excludeReservationId = null, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetUpcomingReservationsByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);
}