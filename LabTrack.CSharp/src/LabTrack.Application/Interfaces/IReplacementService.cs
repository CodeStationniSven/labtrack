using LabTrack.Application.Dtos.Replacement;

namespace LabTrack.Application.Interfaces;

public interface IReplacementService
{
    Task<ReplacementRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ReplacementRequestDto?> GetByTicketNumberAsync(
        string ticketNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReplacementRequestDto>> GetByOriginalAssetIdAsync(
        Guid originalAssetId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReplacementRequestDto>> GetByReplacementAssetIdAsync(
        Guid replacementAssetId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReplacementRequestDto>> GetByStatusAsync(
        string status,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<ReplacementRequestDto> InitiateReplacementAsync(
        CreateReplacementRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<ReplacementRequestDto> UpdateStatusAsync(
        Guid id,
        UpdateReplacementStatusDto dto,
        CancellationToken cancellationToken = default);

    Task ApproveReplacementAsync(
        Guid id,
        Guid approvedByUserId,
        CancellationToken cancellationToken = default);

    Task CompleteReplacementAsync(
        Guid id,
        string completionNotes,
        decimal totalCost,
        CancellationToken cancellationToken = default);

    Task CancelReplacementAsync(
        Guid id,
        string reason,
        CancellationToken cancellationToken = default);
}