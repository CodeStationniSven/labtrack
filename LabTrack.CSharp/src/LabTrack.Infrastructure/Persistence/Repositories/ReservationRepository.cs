using LabTrack.Domain;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly LabTrackDbContext _context;

    public ReservationRepository(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<Reservation> Reservations => _context.Set<Reservation>();

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Include("Laboratory")
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Include("Laboratory")
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await Reservations.AddAsync(reservation, cancellationToken);
        return reservation;
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        Reservations.Update(reservation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        Reservations.Remove(reservation);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Reservation>> GetByReservedUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<Guid>(r, "ReservedByUserId") == userId)
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        return await Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<Guid>(r, "ReservedByUserId") == userId)
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Where(r => EF.Property<Guid>(r, "AssetId") == assetId)
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByLaboratoryIdAsync(Guid laboratoryId, CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Where(r => EF.Property<Guid>(r, "LaboratoryId") == laboratoryId)
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<DateTime>(r, "StartTime") >= startDate
                     && EF.Property<DateTime>(r, "EndTime") <= endDate)
            .OrderBy(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByStatusAsync(string status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        return await Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<string>(r, "Status") == status)
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByWorkstationIdAsync(Guid workstationId, CancellationToken cancellationToken = default)
    {
        return await Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<Guid?>(r, "WorkstationId") == workstationId)
            .OrderByDescending(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetReservationsForTimeSlotAsync(
        Guid laboratoryId,
        Guid? assetId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        var query = Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<Guid>(r, "LaboratoryId") == laboratoryId
                     && EF.Property<DateTime>(r, "StartTime") < endTime
                     && EF.Property<DateTime>(r, "EndTime") > startTime);

        if (assetId.HasValue)
            query = query.Where(r => EF.Property<Guid?>(r, "AssetId") == assetId.Value);

        return await query
            .OrderBy(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingReservationAsync(
        Guid laboratoryId,
        Guid? assetId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Reservations
            .Where(r => EF.Property<Guid>(r, "LaboratoryId") == laboratoryId
                     && EF.Property<DateTime>(r, "StartTime") < endTime
                     && EF.Property<DateTime>(r, "EndTime") > startTime);

        if (assetId.HasValue)
            query = query.Where(r => EF.Property<Guid?>(r, "AssetId") == assetId.Value);

        if (excludeReservationId.HasValue)
            query = query.Where(r => r.Id != excludeReservationId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetUpcomingReservationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await Reservations
            .Include("Laboratory")
            .Where(r => EF.Property<Guid>(r, "ReservedByUserId") == userId
                     && EF.Property<DateTime>(r, "StartTime") >= now)
            .OrderBy(r => EF.Property<DateTime>(r, "StartTime"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}