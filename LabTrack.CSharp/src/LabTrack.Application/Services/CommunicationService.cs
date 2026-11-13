using LabTrack.Application.Dtos.Communications;
using LabTrack.Application.Dtos.Notifications;
using LabTrack.Application.Interfaces;

namespace LabTrack.Application.Services;

public class CommunicationService : ICommunicationService
{
    private readonly IUnitOfWork _unitOfWork;

    public CommunicationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<NotificationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Notification ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<NotificationDto?>(null);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        bool includeRead = false,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return await Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return await Task.FromResult(0);
    }

    public async Task<NotificationDto> SendNotificationAsync(
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return await Task.FromResult(new NotificationDto());
    }

    public async Task BroadcastToRoleAsync(
        string role,
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.", nameof(role));
        }

        ArgumentNullException.ThrowIfNull(dto);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification ID cannot be empty.", nameof(notificationId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    public async Task<ChatMessageDto?> GetMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty.", nameof(id));
        }

        return await Task.FromResult<ChatMessageDto?>(null);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetChannelMessagesAsync(
        Guid channelId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (channelId == Guid.Empty)
        {
            throw new ArgumentException("Channel ID cannot be empty.", nameof(channelId));
        }

        return await Task.FromResult<IReadOnlyList<ChatMessageDto>>(Array.Empty<ChatMessageDto>());
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetDirectMessagesAsync(
        Guid senderId,
        Guid recipientId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (senderId == Guid.Empty || recipientId == Guid.Empty)
        {
            throw new ArgumentException("Sender ID and Recipient ID must be valid.");
        }

        return await Task.FromResult<IReadOnlyList<ChatMessageDto>>(Array.Empty<ChatMessageDto>());
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        SendMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(dto.Content));
        }

        if (dto.ChannelId == null && dto.RecipientId == null)
        {
            throw new ArgumentException("Message must have either a Channel ID or a Recipient ID.");
        }

        return await Task.FromResult(new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            SenderId = dto.SenderId,
            RecipientId = dto.RecipientId,
            ChannelId = dto.ChannelId,
            Content = dto.Content,
            SentAtUtc = DateTime.UtcNow,
            IsRead = false
        });
    }

    public async Task MarkMessageAsReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMessageAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty.", nameof(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}