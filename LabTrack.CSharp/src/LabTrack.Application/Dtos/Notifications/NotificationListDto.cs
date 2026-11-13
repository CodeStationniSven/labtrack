namespace LabTrack.Application.Dtos.Notifications;

public sealed record NotificationListDto
{
    public IReadOnlyCollection<NotificationDto> Items { get; init; } = Array.Empty<NotificationDto>();
    public int TotalCount { get; init; }
    public int UnreadCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage => PageNumber * PageSize < TotalCount;
    public bool HasPreviousPage => PageNumber > 1;
}