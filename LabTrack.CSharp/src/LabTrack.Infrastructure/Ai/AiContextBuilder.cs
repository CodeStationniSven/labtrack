using System.Text;
using LabTrack.Domain.Entities;
using LabTrack.Domain.Enums;

namespace LabTrack.Infrastructure.Ai;

public class AiContextBuilder
{
    private readonly StringBuilder _contextBuilder = new();

    public AiContextBuilder AppendSystemPrompt(string prompt)
    {
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            _contextBuilder.AppendLine($"[SYSTEM INSTRUCTION]");
            _contextBuilder.AppendLine(prompt.Trim());
            _contextBuilder.AppendLine();
        }

        return this;
    }

    public AiContextBuilder AppendUserInformation(User user)
    {
        if (user is null) return this;

        _contextBuilder.AppendLine("[USER CONTEXT]");
        _contextBuilder.AppendLine($"Name: {user.FirstName} {user.LastName}");
        _contextBuilder.AppendLine($"Email: {user.Email}");
        _contextBuilder.AppendLine($"Role: {user.Role}");
        _contextBuilder.AppendLine();

        return this;
    }

    public AiContextBuilder AppendAssetDetails(Asset asset)
    {
        if (asset is null) return this;

        _contextBuilder.AppendLine("[ASSET CONTEXT]");
        _contextBuilder.AppendLine($"Asset Tag: {asset.AssetTag}");
        _contextBuilder.AppendLine($"Name: {asset.Name}");
        _contextBuilder.AppendLine($"Category: {asset.Category}");
        _contextBuilder.AppendLine($"Status: {asset.Status}");
        if (!string.IsNullOrWhiteSpace(asset.SerialNumber))
        {
            _contextBuilder.AppendLine($"Serial Number: {asset.SerialNumber}");
        }
        _contextBuilder.AppendLine();

        return this;
    }

    public AiContextBuilder AppendRepairTicketContext(RepairTicket ticket)
    {
        if (ticket is null) return this;

        _contextBuilder.AppendLine("[REPAIR TICKET CONTEXT]");
        _contextBuilder.AppendLine($"Ticket ID: {ticket.Id}");
        _contextBuilder.AppendLine($"Title: {ticket.Title}");
        _contextBuilder.AppendLine($"Priority: {ticket.Priority}");
        _contextBuilder.AppendLine($"Status: {ticket.Status}");
        _contextBuilder.AppendLine($"Description: {ticket.Description}");
        _contextBuilder.AppendLine();

        return this;
    }

    public AiContextBuilder AppendCustomContext(string header, string details)
    {
        if (string.IsNullOrWhiteSpace(details)) return this;

        _contextBuilder.AppendLine($"[{header.ToUpperInvariant()}]");
        _contextBuilder.AppendLine(details.Trim());
        _contextBuilder.AppendLine();

        return this;
    }

    public string Build()
    {
        return _contextBuilder.ToString().TrimEnd();
    }

    public void Clear()
    {
        _contextBuilder.Clear();
    }
}