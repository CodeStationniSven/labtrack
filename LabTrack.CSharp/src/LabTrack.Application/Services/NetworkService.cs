using LabTrack.Application.Dtos.Network;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class NetworkService : INetworkService
{
    private readonly IUnitOfWork _unitOfWork;

    public NetworkService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NetworkDeviceDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        return await Task.FromResult<NetworkDeviceDto?>(null);
    }

    public async Task<NetworkDeviceDto?> GetByIpAddressAsync(
        string ipAddress, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty.", nameof(ipAddress));

        return await Task.FromResult<NetworkDeviceDto?>(null);
    }

    public async Task<NetworkDeviceDto?> GetByMacAddressAsync(
        string macAddress, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException("MAC address cannot be empty.", nameof(macAddress));

        return await Task.FromResult<NetworkDeviceDto?>(null);
    }


    public async Task<IReadOnlyList<NetworkDeviceDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default)
    {
        if (laboratoryId == Guid.Empty)
            throw new ArgumentException("Laboratory ID cannot be empty.", nameof(laboratoryId));

        return await Task.FromResult<IReadOnlyList<NetworkDeviceDto>>(Array.Empty<NetworkDeviceDto>());
    }


    public async Task<IReadOnlyList<NetworkDeviceDto>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default)
    {
        if (workstationId == Guid.Empty)
            throw new ArgumentException("Workstation ID cannot be empty.", nameof(workstationId));

        return await Task.FromResult<IReadOnlyList<NetworkDeviceDto>>(Array.Empty<NetworkDeviceDto>());
    }


    public async Task<IReadOnlyList<NetworkDeviceDto>> GetActiveDevicesAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult<IReadOnlyList<NetworkDeviceDto>>(Array.Empty<NetworkDeviceDto>());
    }

    public async Task<IReadOnlyList<NetworkDeviceDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty.", nameof(status));

        return await Task.FromResult<IReadOnlyList<NetworkDeviceDto>>(Array.Empty<NetworkDeviceDto>());
    }


    public async Task<NetworkDeviceDto> RegisterDeviceAsync(
        RegisterNetworkDeviceDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!string.IsNullOrWhiteSpace(dto.IpAddress) &&
            !await IsIpAddressUniqueAsync(dto.IpAddress, null, cancellationToken))
        {
            throw new InvalidOperationException($"IP address '{dto.IpAddress}' is already assigned to another network device.");
        }

        if (!string.IsNullOrWhiteSpace(dto.MacAddress) &&
            !await IsMacAddressUniqueAsync(dto.MacAddress, null, cancellationToken))
        {
            throw new InvalidOperationException($"MAC address '{dto.MacAddress}' is already registered.");
        }

        return await Task.FromResult(new NetworkDeviceDto
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            IpAddress = dto.IpAddress,
            MacAddress = dto.MacAddress,
            DeviceType = dto.DeviceType,
            SubnetMask = dto.SubnetMask,
            Gateway = dto.Gateway,
            Status = "Online",
            LaboratoryId = dto.LaboratoryId,
            WorkstationId = dto.WorkstationId,
            LastPingUtc = DateTime.UtcNow
        });
    }

    public async Task<NetworkDeviceDto> UpdateDeviceAsync(
        Guid id, 
        UpdateNetworkDeviceDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(dto);

        if (!string.IsNullOrWhiteSpace(dto.IpAddress) &&
            !await IsIpAddressUniqueAsync(dto.IpAddress, id, cancellationToken))
        {
            throw new InvalidOperationException($"IP address '{dto.IpAddress}' is already assigned to another network device.");
        }

        if (!string.IsNullOrWhiteSpace(dto.MacAddress) &&
            !await IsMacAddressUniqueAsync(dto.MacAddress, id, cancellationToken))
        {
            throw new InvalidOperationException($"MAC address '{dto.MacAddress}' is already registered.");
        }

        return await Task.FromResult(new NetworkDeviceDto
        {
            Id = id,
            Name = dto.Name ?? string.Empty,
            IpAddress = dto.IpAddress,
            MacAddress = dto.MacAddress,
            DeviceType = dto.DeviceType ?? "Switch",
            SubnetMask = dto.SubnetMask,
            Gateway = dto.Gateway,
            Status = dto.Status ?? "Active",
            LaboratoryId = dto.LaboratoryId,
            WorkstationId = dto.WorkstationId
        });
    }

    public async Task UpdateStatusAsync(
        Guid id, 
        string status, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty.", nameof(status));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    public async Task UpdateDeviceStatusAsync(
        Guid id, 
        string status, 
        DateTime lastSeenUtc, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty.", nameof(status));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordPingAsync(
        Guid id, 
        bool isOnline, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    public async Task UnregisterDeviceAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDeviceAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Network Device ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsIpAddressUniqueAsync(
        string ipAddress, 
        Guid? excludeDeviceId = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return await Task.FromResult(true);
    }

    public async Task<bool> IsMacAddressUniqueAsync(
        string macAddress, 
        Guid? excludeDeviceId = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        return await Task.FromResult(true);
    }
}