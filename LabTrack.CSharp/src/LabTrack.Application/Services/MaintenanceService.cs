using System.Reflection;
using LabTrack.Application.Dtos.Maintenance;
using LabTrack.Application.Interfaces;
using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;

namespace LabTrack.Application.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MaintenanceService(IMaintenanceRepository maintenanceRepository, IUnitOfWork unitOfWork)
    {
        _maintenanceRepository = maintenanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MaintenanceScheduleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _maintenanceRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapObject<MaintenanceScheduleDto>(entity);
    }

    public async Task<IReadOnlyList<MaintenanceScheduleDto>> GetByLaboratoryIdAsync(Guid laboratoryId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var all = await _maintenanceRepository.GetAllAsync(cancellationToken);
        var safePage = pageNumber < 1 ? 1 : pageNumber;
        var safeSize = pageSize < 1 ? 20 : pageSize;

        return all
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(MapObject<MaintenanceScheduleDto>)
            .ToList();
    }

    public async Task<IReadOnlyList<MaintenanceScheduleDto>> GetByAssetIdAsync(Guid assetId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var all = await _maintenanceRepository.GetAllAsync(cancellationToken);
        var safePage = pageNumber < 1 ? 1 : pageNumber;
        var safeSize = pageSize < 1 ? 20 : pageSize;

        return all
            .Where(m => m.AssetId == assetId)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(MapObject<MaintenanceScheduleDto>)
            .ToList();
    }

    public async Task<IReadOnlyList<MaintenanceScheduleDto>> GetUpcomingSchedulesAsync(Guid? laboratoryId = null, int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var all = await _maintenanceRepository.GetAllAsync(cancellationToken);
        return all.Select(MapObject<MaintenanceScheduleDto>).ToList();
    }

    public async Task<IReadOnlyList<MaintenanceScheduleDto>> GetOverdueSchedulesAsync(Guid? laboratoryId = null, CancellationToken cancellationToken = default)
    {
        var all = await _maintenanceRepository.GetAllAsync(cancellationToken);
        return all.Select(MapObject<MaintenanceScheduleDto>).ToList();
    }

    public async Task<MaintenanceScheduleDto> CreateScheduleAsync(CreateMaintenanceScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var entity = MapObject<MaintenanceSchedule>(dto);
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        entity.CreatedAtUtc = DateTime.UtcNow;

        await _maintenanceRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObject<MaintenanceScheduleDto>(entity);
    }

    public async Task<MaintenanceScheduleDto> UpdateScheduleAsync(Guid id, UpdateMaintenanceScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _maintenanceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"MaintenanceSchedule {id} not found.");

        CopyProperties(dto, existing);
        existing.LastModifiedAtUtc = DateTime.UtcNow;

        await _maintenanceRepository.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObject<MaintenanceScheduleDto>(existing);
    }

    public async Task CompleteScheduleAsync(Guid id, string completionNotes, CancellationToken cancellationToken = default)
    {
        var existing = await _maintenanceRepository.GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            existing.LastModifiedAtUtc = DateTime.UtcNow;
            await _maintenanceRepository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CancelScheduleAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var existing = await _maintenanceRepository.GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            await _maintenanceRepository.DeleteAsync(existing, cancellationToken);
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
