using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface INetworkRepository : IGenericRepository<NetworkDevice>
{
    Task<NetworkDevice?> GetByMacAddressAsync(
        string macAddress, 
        CancellationToken cancellationToken = default);

    Task<NetworkDevice?> GetByIpAddressAsync(
        string ipAddress, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDevice>> GetByLaboratoryIdAsync(
        Guid laboratoryId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDevice>> GetByWorkstationIdAsync(
        Guid workstationId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDevice>> GetByStatusAsync(
        string status, 
        CancellationToken cancellationToken = default);

    Task<bool> IsMacAddressUniqueAsync(
        string macAddress, 
        Guid? excludeDeviceId = null, 
        CancellationToken cancellationToken = default);

    Task<bool> IsIpAddressUniqueAsync(
        string ipAddress, 
        Guid? excludeDeviceId = null, 
        CancellationToken cancellationToken = default);
}