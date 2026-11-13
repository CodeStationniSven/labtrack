using LabTrack.Application.Dtos.Qr;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class QrService : IQrService
{
    private readonly IQrGeneratorEngine _qrGeneratorEngine;
    private readonly IQrTokenService _qrTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public QrService(
        IQrGeneratorEngine qrGeneratorEngine,
        IQrTokenService qrTokenService,
        IUnitOfWork unitOfWork)
    {
        _qrGeneratorEngine = qrGeneratorEngine;
        _qrTokenService = qrTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<QrCodeDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("QR code ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<QrCodeDto?>(null);
    }

    public async Task<QrCodeDto?> GetByCodeDataAsync(
        string codeData, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeData))
        {
            throw new ArgumentException("Code data cannot be empty.", nameof(codeData));
        }

        return await Task.FromResult<QrCodeDto?>(null);
    }

    public async Task<QrCodeDto?> GetByEntityAsync(
        string entityType, 
        Guid entityId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type cannot be empty.", nameof(entityType));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));
        }

        return await Task.FromResult<QrCodeDto?>(null);
    }

    public async Task<QrCodeDto> GenerateEntityQrAsync(
        GenerateQrDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.EntityType))
        {
            throw new ArgumentException("Entity type must be provided.", nameof(dto.EntityType));
        }

        if (dto.EntityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID must be valid.", nameof(dto.EntityId));
        }

        var token = _qrTokenService.GenerateToken(dto.EntityType, dto.EntityId);
        var base64Image = _qrGeneratorEngine.GenerateBase64Png(token);

        var qrDto = new QrCodeDto
        {
            Id = Guid.NewGuid(),
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            CodeData = token,
            Base64Image = base64Image,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return qrDto;
    }

    public async Task<byte[]> ExportQrImageAsync(
        Guid id, 
        string format = "png", 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("QR code ID cannot be empty.", nameof(id));
        }

        var qr = await GetByIdAsync(id, cancellationToken);
        if (qr == null)
        {
            throw new KeyNotFoundException($"QR code with ID '{id}' was not found.");
        }

        return format.ToLowerInvariant() switch
        {
            "svg" => _qrGeneratorEngine.GenerateSvg(qr.CodeData),
            _ => _qrGeneratorEngine.GeneratePng(qr.CodeData)
        };
    }

    public async Task DeactivateQrAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("QR code ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ValidateQrDataAsync(
        string codeData, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeData))
        {
            return false;
        }

        return await Task.FromResult(_qrTokenService.ValidateToken(codeData));
    }
}