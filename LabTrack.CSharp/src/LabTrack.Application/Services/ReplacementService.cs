using LabTrack.Application.Dtos.Replacement;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class ReplacementService : IReplacementService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReplacementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReplacementRequestDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Replacement record ID cannot be empty.", nameof(id));

        return await Task.FromResult<ReplacementRequestDto?>(null);
    }

    public async Task<ReplacementRequestDto?> GetByTicketNumberAsync(
        string ticketNumber, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new ArgumentException("Ticket number cannot be empty.", nameof(ticketNumber));

        return await Task.FromResult<ReplacementRequestDto?>(null);
    }

    public async Task<IReadOnlyList<ReplacementRequestDto>> GetByOriginalAssetIdAsync(
        Guid originalAssetId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (originalAssetId == Guid.Empty)
            throw new ArgumentException("Original asset ID cannot be empty.", nameof(originalAssetId));

        return await Task.FromResult<IReadOnlyList<ReplacementRequestDto>>(Array.Empty<ReplacementRequestDto>());
    }

    public async Task<IReadOnlyList<ReplacementRequestDto>> GetByReplacementAssetIdAsync(
        Guid replacementAssetId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (replacementAssetId == Guid.Empty)
            throw new ArgumentException("Replacement asset ID cannot be empty.", nameof(replacementAssetId));

        return await Task.FromResult<IReadOnlyList<ReplacementRequestDto>>(Array.Empty<ReplacementRequestDto>());
    }

    public async Task<IReadOnlyList<ReplacementRequestDto>> GetByStatusAsync(
        string status, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty.", nameof(status));

        return await Task.FromResult<IReadOnlyList<ReplacementRequestDto>>(Array.Empty<ReplacementRequestDto>());
    }

    public async Task<ReplacementRequestDto> InitiateReplacementAsync(
        CreateReplacementRequestDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.OriginalAssetId == Guid.Empty)
            throw new ArgumentException("Original asset ID must be provided.", nameof(dto.OriginalAssetId));

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("Replacement reason cannot be empty.", nameof(dto.Reason));

        return await Task.FromResult(new ReplacementRequestDto
        {
            Id = Guid.NewGuid(),
            OriginalAssetId = dto.OriginalAssetId,
            ReplacementAssetId = dto.ReplacementAssetId,
            Reason = dto.Reason,
            Status = "Initiated",
            InitiatedByUserId = dto.InitiatedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<ReplacementRequestDto> UpdateStatusAsync(
        Guid id, 
        UpdateReplacementStatusDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Replacement record ID cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Status))
            throw new ArgumentException("Status cannot be empty.", nameof(dto.Status));

        return await Task.FromResult(new ReplacementRequestDto
        {
            Id = id,
            Status = dto.Status,
            Notes = dto.Notes
        });
    }

    public async Task ApproveReplacementAsync(
        Guid id, 
        Guid approvedByUserId, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || approvedByUserId == Guid.Empty)
            throw new ArgumentException("Replacement record ID and Approver User ID must be valid.");

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteReplacementAsync(
        Guid id, 
        string completionNotes, 
        decimal totalCost, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Replacement record ID cannot be empty.", nameof(id));

        if (totalCost < 0)
            throw new ArgumentException("Total replacement cost cannot be negative.", nameof(totalCost));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelReplacementAsync(
        Guid id, 
        string reason, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Replacement record ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason must be provided.", nameof(reason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}