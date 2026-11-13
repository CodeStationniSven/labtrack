using System.Reflection;
using LabTrack.Application.Dtos.Tickets;
using LabTrack.Application.Interfaces;
using LabTrack.Domain.Entities;

namespace LabTrack.Application.Services;

public class RepairTicketService : IRepairTicketService
{
    private readonly IUnitOfWork _unitOfWork;

    public RepairTicketService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RepairTicketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _unitOfWork.RepairTickets.GetByIdAsync(id, cancellationToken);
        return ticket is null ? null : MapObject<RepairTicketDto>(ticket);
    }

    public async Task<RepairTicketDto?> GetByTicketNumberAsync(string ticketNumber, CancellationToken cancellationToken = default)
    {
        var ticket = await _unitOfWork.RepairTickets.GetByTicketNumberAsync(ticketNumber, cancellationToken);
        return ticket is null ? null : MapObject<RepairTicketDto>(ticket);
    }

    public async Task<IReadOnlyList<RepairTicketDto>> GetByAssetIdAsync(Guid assetId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var list = await _unitOfWork.RepairTickets.GetByAssetIdAsync(assetId, cancellationToken);
        var safePage = pageNumber < 1 ? 1 : pageNumber;
        var safeSize = pageSize < 1 ? 20 : pageSize;

        return list
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(MapObject<RepairTicketDto>)
            .ToList();
    }

    public async Task<IReadOnlyList<RepairTicketDto>> GetByTechnicianIdAsync(Guid technicianId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var list = await _unitOfWork.RepairTickets.GetByAssignedTechnicianIdAsync(technicianId, cancellationToken);
        var safePage = pageNumber < 1 ? 1 : pageNumber;
        var safeSize = pageSize < 1 ? 20 : pageSize;

        return list
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(MapObject<RepairTicketDto>)
            .ToList();
    }

    public async Task<IReadOnlyList<RepairTicketDto>> GetByStatusAsync(string status, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var all = await _unitOfWork.RepairTickets.GetAllAsync(cancellationToken);
        var safePage = pageNumber < 1 ? 1 : pageNumber;
        var safeSize = pageSize < 1 ? 20 : pageSize;

        return all
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(MapObject<RepairTicketDto>)
            .ToList();
    }

    public async Task<RepairTicketDto> CreateAsync(CreateRepairTicketDto dto, CancellationToken cancellationToken = default)
    {
        var entity = MapObject<RepairTicket>(dto);
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        entity.CreatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.RepairTickets.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObject<RepairTicketDto>(entity);
    }

    public async Task<RepairTicketDto> UpdateStatusAsync(Guid id, UpdateRepairTicketStatusDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.RepairTickets.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"RepairTicket {id} not found.");

        CopyProperties(dto, existing);
        existing.LastModifiedAtUtc = DateTime.UtcNow;

        await _unitOfWork.RepairTickets.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObject<RepairTicketDto>(existing);
    }

    public async Task AssignTechnicianAsync(Guid id, Guid technicianId, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.RepairTickets.GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            existing.AssignedToUserId = technicianId;
            existing.LastModifiedAtUtc = DateTime.UtcNow;
            await _unitOfWork.RepairTickets.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CompleteRepairAsync(Guid id, string resolutionNotes, decimal partsCost, decimal laborCost, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.RepairTickets.GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            existing.LastModifiedAtUtc = DateTime.UtcNow;
            await _unitOfWork.RepairTickets.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CancelTicketAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.RepairTickets.GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            await _unitOfWork.RepairTickets.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static TDest MapObject<TDest>(object source) where TDest : new()
    {
        var dest = new TDest();
        CopyProperties(source, dest);
        return dest;
    }

    private static void CopyProperties(object source, object target)
    {
        var srcProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dstProps = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

        foreach (var dp in dstProps)
        {
            var sp = srcProps.FirstOrDefault(x => string.Equals(x.Name, dp.Name, StringComparison.OrdinalIgnoreCase));
            if (sp != null)
            {
                var val = sp.GetValue(source);
                if (val != null)
                {
                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(dp.PropertyType) ?? dp.PropertyType;
                        if (targetType.IsInstanceOfType(val)) dp.SetValue(target, val);
                        else if (targetType.IsEnum && val is string s && Enum.TryParse(targetType, s, true, out var ev)) dp.SetValue(target, ev);
                        else if (targetType == typeof(string)) dp.SetValue(target, val.ToString());
                    }
                    catch { }
                }
            }
        }
    }
}
