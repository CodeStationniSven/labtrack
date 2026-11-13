using LabTrack.Application.Dtos.Notifications;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationDto?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Notification ID cannot be empty.", nameof(id));

        return await Task.FromResult<NotificationDto?>(null);
    }


    public async Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(
        Guid userId, 
        bool includeRead = false,
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return await Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
    }


    public async Task<int> GetUnreadCountByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return await Task.FromResult(0);
    }


    public async Task<NotificationDto> CreateNotificationAsync(
        CreateNotificationDto dto, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.UserId == Guid.Empty)
            throw new ArgumentException("Target User ID must be provided.", nameof(dto.UserId));

        if (string.IsNullOrWhiteSpace(dto.Message))
            throw new ArgumentException("Notification message cannot be empty.", nameof(dto.Message));

        return await Task.FromResult(new NotificationDto
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        });
    }


    public async Task SendToUserAsync(
        Guid userId, 
        string title, 
        string message, 
        string type = "Info",
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    public async Task SendToRoleAsync(
        string role, 
        string title, 
        string message, 
        string type = "Info",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty.", nameof(role));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Notification ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    public async Task MarkAllAsReadAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Notification ID cannot be empty.", nameof(id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
}