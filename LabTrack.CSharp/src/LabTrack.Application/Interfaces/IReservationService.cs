using LabTrack.Application.Dtos.Reservations;

namespace LabTrack.Application.Interfaces;

public interface IReservationService
{
    Task<ReservationDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetByUserIdAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetByWorkstationIdAsync(
        Guid workstationId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetActiveReservationsAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CreateReservationAsync(
        CreateReservationDto dto, 
        CancellationToken cancellationToken = default);

    Task<ReservationDto> UpdateReservationAsync(
        Guid id, 
        UpdateReservationDto dto, 
        CancellationToken cancellationToken = default);

    Task ApproveReservationAsync(
        Guid id, 
        Guid approvedByUserId, 
        CancellationToken cancellationToken = default);

    Task RejectReservationAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default);

    Task CancelReservationAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default);

    Task CheckInAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task CheckOutAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<bool> HasScheduleConflictAsync(
        Guid workstationId, 
        DateTime startTimeUtc, 
        DateTime endTimeUtc, 
        Guid? excludeReservationId = null, 
        CancellationToken cancellationToken = default);

}