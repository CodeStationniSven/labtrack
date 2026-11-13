using System.Collections;
using System.Reflection;
using LabTrack.Application.Dtos.Laboratories;
using LabTrack.Application.Dtos.Workstations;
using LabTrack.Application.Interfaces;
using LabTrack.Domain.Entities;

namespace LabTrack.Application.Services;

public class LaboratoryService : ILaboratoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public LaboratoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LaboratoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lab = await _unitOfWork.Laboratories.GetByIdAsync(id, cancellationToken);
        return lab is null ? null : MapObject<LaboratoryDto>(lab);
    }

    public async Task<LaboratoryDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var lab = await _unitOfWork.Laboratories.GetByCodeAsync(code, cancellationToken);
        return lab is null ? null : MapObject<LaboratoryDto>(lab);
    }

    public async Task<IReadOnlyList<LaboratoryDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var labs = await _unitOfWork.Laboratories.GetAllAsync(cancellationToken);
        var safePage = pageNumber < 1 ? 1 : pageNumber;
        var safeSize = pageSize < 1 ? 20 : pageSize;

        return labs
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(MapObject<LaboratoryDto>)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkstationDto>> GetWorkstationsByLaboratoryIdAsync(Guid laboratoryId, CancellationToken cancellationToken = default)
    {
        var lab = await _unitOfWork.Laboratories.GetByIdAsync(laboratoryId, cancellationToken);
        if (lab is null) return Array.Empty<WorkstationDto>();

        var wsProp = lab.GetType().GetProperty("Workstations", BindingFlags.Public | BindingFlags.Instance);
        if (wsProp?.GetValue(lab) is IEnumerable wsList)
        {
            var result = new List<WorkstationDto>();
            foreach (var item in wsList)
            {
                if (item != null) result.Add(MapObject<WorkstationDto>(item));
            }
            return result;
        }

        return Array.Empty<WorkstationDto>();
    }

    public async Task<LaboratoryDto> CreateAsync(CreateLaboratoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = MapObject<Laboratory>(dto);
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        entity.CreatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.Laboratories.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObject<LaboratoryDto>(entity);
    }

    public async Task<LaboratoryDto> UpdateAsync(Guid id, UpdateLaboratoryDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Laboratories.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Laboratory with ID {id} was not found.");

        CopyProperties(dto, existing);
        existing.LastModifiedAtUtc = DateTime.UtcNow;

        await _unitOfWork.Laboratories.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObject<LaboratoryDto>(existing);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Laboratories.GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            await _unitOfWork.Laboratories.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Laboratories.IsCodeUniqueAsync(code, excludeId, cancellationToken);
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
