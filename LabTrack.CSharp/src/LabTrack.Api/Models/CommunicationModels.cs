using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class SendMessageRequest
{
    [Required]
    public string RecipientUserId { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
}

public class CommunicationResponse
{
    public string Id { get; set; } = string.Empty;
    public string SenderUserId { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
