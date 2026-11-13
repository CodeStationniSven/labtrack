using LabTrack.Domain.Entities;

namespace LabTrack.Application.Interfaces.Repositories;

public interface IAiChatRepository : IGenericRepository<AiChatMessage>
{
    Task<IReadOnlyList<AiChatMessage>> GetChatHistoryBySessionIdAsync(
        Guid sessionId, 
        int maxMessages = 50, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiChatMessage>> GetUserChatHistoryAsync(
        Guid userId, 
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default);

    Task DeleteSessionHistoryAsync(
        Guid sessionId, 
        CancellationToken cancellationToken = default);
}