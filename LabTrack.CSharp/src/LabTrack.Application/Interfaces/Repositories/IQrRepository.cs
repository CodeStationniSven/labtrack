using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IQrRepository : IGenericRepository<QrCode>
{
    Task<QrCode?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<QrCode?> GetActiveByWorkstationIdAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QrCode>> GetActiveByLaboratoryIdAsync(
        Guid laboratoryId,
        CancellationToken cancellationToken = default);

    Task<bool> IsTokenValidAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task DeactivateExistingTokensForWorkstationAsync(
        Guid workstationId,
        CancellationToken cancellationToken = default);

    Task RevokeExpiredTokensAsync(
        DateTime expiryThresholdUtc,
        CancellationToken cancellationToken = default);
}