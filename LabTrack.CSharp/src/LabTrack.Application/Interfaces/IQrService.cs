using LabTrack.Application.Dtos.Qr;

namespace LabTrack.Application.Interfaces;

public interface IQrService
{
    Task<QrCodeDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<QrCodeDto?> GetByCodeDataAsync(
        string codeData, 
        CancellationToken cancellationToken = default);

    Task<QrCodeDto?> GetByEntityAsync(
        string entityType, 
        Guid entityId, 
        CancellationToken cancellationToken = default);

    Task<QrCodeDto> GenerateEntityQrAsync(
        GenerateQrDto dto, 
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportQrImageAsync(
        Guid id, 
        string format = "png", 
        CancellationToken cancellationToken = default);

    Task DeactivateQrAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<bool> ValidateQrDataAsync(
        string codeData, 
        CancellationToken cancellationToken = default);
}