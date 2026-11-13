using LabTrack.Application.Dtos.Network;

namespace LabTrack.Application.Interfaces;

public interface INetworkService
{
    Task<NetworkDeviceDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<NetworkDeviceDto?> GetByMacAddressAsync(
        string macAddress, 
        CancellationToken cancellationToken = default);

    Task<NetworkDeviceDto?> GetByIpAddressAsync(
        string ipAddress, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDeviceDto>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDeviceDto>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDeviceDto>> GetActiveDevicesAsync(
        Guid? laboratoryId = null, 
        CancellationToken cancellationToken = default);

    Task<NetworkDeviceDto> RegisterDeviceAsync(
        RegisterNetworkDeviceDto dto, 
        CancellationToken cancellationToken = default);

    Task<NetworkDeviceDto> UpdateDeviceAsync(
        Guid id, 
        UpdateNetworkDeviceDto dto, 
        CancellationToken cancellationToken = default);

    Task UpdateDeviceStatusAsync(
        Guid id, 
        string status, 
        DateTime lastSeenUtc, 
        CancellationToken cancellationToken = default);

    Task UnregisterDeviceAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<bool> IsMacAddressUniqueAsync(
        string macAddress, 
        Guid? excludeDeviceId = null, 
        CancellationToken cancellationToken = default);
}