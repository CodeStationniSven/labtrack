using LabTrack.Application.Dtos.Reservations;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReservationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservationDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));

        return await Task.FromResult<ReservationDto?>(null);
    }

    public async Task<IReadOnlyList<ReservationDto>> GetByUserIdAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return await Task.FromResult<IReadOnlyList<ReservationDto>>(Array.Empty<ReservationDto>());
    }

    public async Task<IReadOnlyList<ReservationDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (laboratoryId == Guid.Empty)
            throw new ArgumentException("Laboratory ID cannot be empty.", nameof(laboratoryId));

        return await Task.FromResult<IReadOnlyList<ReservationDto>>(Array.Empty<ReservationDto>());
    }

    public async Task<IReadOnlyList<ReservationDto>> GetByWorkstationIdAsync(
        Guid workstationId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (workstationId == Guid.Empty)
            throw new ArgumentException("Workstation ID cannot be empty.", nameof(workstationId));

        return await Task.FromResult<IReadOnlyList<ReservationDto>>(Array.Empty<ReservationDto>());
    }

    public async Task<IReadOnlyList<ReservationDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty.", nameof(status));

        return await Task.FromResult<IReadOnlyList<ReservationDto>>(Array.Empty<ReservationDto>());
    }

    public async Task<IReadOnlyList<ReservationDto>> GetActiveReservationsAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult<IReadOnlyList<ReservationDto>>(Array.Empty<ReservationDto>());
    }

    public async Task<ReservationDto> CreateReservationAsync(
        CreateReservationDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.UserId == Guid.Empty)
            throw new ArgumentException("User ID must be provided.", nameof(dto.UserId));

        if (!dto.WorkstationId.HasValue || dto.WorkstationId.Value == Guid.Empty)
            throw new ArgumentException("Workstation ID must be provided.", nameof(dto.WorkstationId));

        if (dto.StartTimeUtc >= dto.EndTimeUtc)
            throw new ArgumentException("Start time must be earlier than end time.");

        if (dto.StartTimeUtc < DateTime.UtcNow)
            throw new ArgumentException("Reservation start time cannot be in the past.");

        if (await HasScheduleConflictAsync(dto.WorkstationId.Value, dto.StartTimeUtc, dto.EndTimeUtc, null, cancellationToken))
            throw new InvalidOperationException("The requested workstation is already reserved for the specified time slot.");

        return await Task.FromResult(new ReservationDto
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            LaboratoryId = dto.LaboratoryId,
            WorkstationId = dto.WorkstationId,
            StartTimeUtc = dto.StartTimeUtc,
            EndTimeUtc = dto.EndTimeUtc,
            Purpose = dto.Purpose,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<ReservationDto> UpdateReservationAsync(
        Guid id, 
        UpdateReservationDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(dto);

        if (dto.StartTimeUtc >= dto.EndTimeUtc)
            throw new ArgumentException("Start time must be earlier than end time.");

        if (dto.WorkstationId.HasValue &&
            await HasScheduleConflictAsync(dto.WorkstationId.Value, dto.StartTimeUtc, dto.EndTimeUtc, id, cancellationToken))
        {
            throw new InvalidOperationException("The requested workstation has a schedule conflict during the updated time slot.");
        }

        return await Task.FromResult(new ReservationDto
        {
            Id = id,
            WorkstationId = dto.WorkstationId,
            StartTimeUtc = dto.StartTimeUtc,
            EndTimeUtc = dto.EndTimeUtc,
            Purpose = dto.Purpose,
            Status = dto.Status ?? "Pending"
        });
    }

    public async Task ApproveReservationAsync(
        Guid id, 
        Guid approvedByUserId, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || approvedByUserId == Guid.Empty)
            throw new ArgumentException("Reservation ID and Approver User ID must be valid.");

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectReservationAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason must be provided.", nameof(reason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelReservationAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason must be provided.", nameof(reason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckInAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckOutAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Reservation ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasScheduleConflictAsync(
        Guid workstationId, 
        DateTime startTimeUtc, 
        DateTime endTimeUtc, 
        Guid? excludeReservationId = null, 
        CancellationToken cancellationToken = default)
    {
        if (workstationId == Guid.Empty)
            return false;

        return await Task.FromResult(false);
    }
    
}