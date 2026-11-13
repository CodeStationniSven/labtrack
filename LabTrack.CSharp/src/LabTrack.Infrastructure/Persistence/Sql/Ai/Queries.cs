namespace LabTrack.Infrastructure.Persistence.Sql.Ai;

public static class Queries
{
    public const string GetSessionWithMessages = @"
        SELECT 
            s.Id,
            s.UserId,
            s.Title,
            s.CreatedAt,
            s.UpdatedAt,
            m.Id,
            m.SessionId,
            m.Role,
            m.Content,
            m.TokensUsed,
            m.CreatedAt
        FROM AiChatSessions s
        LEFT JOIN AiChatMessages m ON s.Id = m.SessionId
        WHERE s.Id = @SessionId
        ORDER BY m.CreatedAt ASC;";

    public const string GetUserSessionsOverview = @"
        SELECT 
            s.Id,
            s.UserId,
            s.Title,
            s.CreatedAt,
            s.UpdatedAt,
            COUNT(m.Id) AS MessageCount,
            MAX(m.CreatedAt) AS LastActivity
        FROM AiChatSessions s
        LEFT JOIN AiChatMessages m ON s.Id = m.SessionId
        WHERE s.UserId = @UserId
        GROUP BY s.Id, s.UserId, s.Title, s.CreatedAt, s.UpdatedAt
        ORDER BY s.UpdatedAt DESC;";

    public const string GetSessionTokenUsage = @"
        SELECT 
            SessionId,
            SUM(TokensUsed) AS TotalTokens,
            COUNT(1) AS TotalMessages
        FROM AiChatMessages
        WHERE SessionId = @SessionId
        GROUP BY SessionId;";

    public const string SearchChatHistory = @"
        SELECT 
            m.Id AS MessageId,
            m.SessionId,
            s.Title AS SessionTitle,
            m.Role,
            m.Content,
            m.CreatedAt
        FROM AiChatMessages m
        INNER JOIN AiChatSessions s ON m.SessionId = s.Id
        WHERE s.UserId = @UserId 
          AND m.Content LIKE '%' + @SearchTerm + '%'
        ORDER BY m.CreatedAt DESC;";
}