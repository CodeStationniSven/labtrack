using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class SendAiMessageRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
}

public class CreateAiSessionRequest
{
    public string Title { get; set; } = "New AI Conversation";
}

public class AiChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public string Sender { get; set; } = "Assistant";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
