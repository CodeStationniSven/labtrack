using LabTrack.Application.Interfaces;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabTrack.Api.Controllers;

// â•â•â•â•â•â•â•â•â•â•â• CHAIN OF CUSTODY â•â•â•â•â•â•â•â•â•â•â•
[ApiController, Route("api/v1/chain-of-custody"), Authorize]
public class ChainOfCustodyController : ControllerBase
{
    private readonly LabTrackDbContext _d;
    public ChainOfCustodyController(LabTrackDbContext d) => _d = d;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? assetId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var q = _d.ChainOfCustodyEvents.AsNoTracking().AsQueryable();
        if (assetId.HasValue) q = q.Where(x => x.AssetId == assetId.Value);
        var t = await q.CountAsync();
        var i = await q.OrderByDescending(x => x.TransferredAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items = i, totalCount = t, page, pageSize } });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChainEventReq r)
    {
        var u = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(u, out var uid)) return Unauthorized(new { success = false });
        var x = new ChainOfCustodyEvent
        {
            Id = Guid.NewGuid(),
            AssetId = r.AssetId,
            ReleasedByUserId = uid,
            ReceivedByUserId = r.ReceivedByUserId == Guid.Empty ? uid : r.ReceivedByUserId,
            TransferType = r.TransferType ?? "Internal",
            Reason = r.Reason ?? "",
            TransferredAtUtc = DateTime.UtcNow,
            Notes = r.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };
        _d.ChainOfCustodyEvents.Add(x);
        await _d.SaveChangesAsync();
        return Ok(new { success = true, data = x });
    }

    [HttpGet("by-asset/{assetId:guid}")]
    public async Task<IActionResult> ByAsset(Guid assetId)
    {
        var list = await _d.ChainOfCustodyEvents.AsNoTracking()
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.TransferredAtUtc).ToListAsync();
        return Ok(new { success = true, data = list });
    }
}
public record ChainEventReq(Guid AssetId, Guid ReceivedByUserId, string? TransferType, string? Reason, string? Notes);

// â•â•â•â•â•â•â•â•â•â•â• QR SESSIONS â•â•â•â•â•â•â•â•â•â•â•
[ApiController, Route("api/v1/qr-sessions"), Authorize]
public class QrSessionController : ControllerBase
{
    private readonly LabTrackDbContext _d;
    public QrSessionController(LabTrackDbContext d) => _d = d;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        var q = _d.QrSessions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        var t = await q.CountAsync();
        var i = await q.OrderByDescending(x => x.IssuedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { success = true, data = new { items = i, totalCount = t, page, pageSize } });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QrSessionReq r)
    {
        var u = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(u, out var uid)) return Unauthorized(new { success = false });
        var x = new QrSession
        {
            Id = Guid.NewGuid(),
            Token = "QRSESS-" + Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper(),
            UserId = r.UserId == Guid.Empty ? uid : r.UserId,
            ResourceId = r.ResourceId,
            SessionType = r.SessionType ?? "AUTH",
            Status = "ACTIVE",
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(8),
            Notes = r.Notes,
            CreatedAtUtc = DateTime.UtcNow
        };
        _d.QrSessions.Add(x);
        await _d.SaveChangesAsync();
        return Ok(new { success = true, data = x });
    }
}
public record QrSessionReq(Guid UserId, Guid? ResourceId, string? SessionType, string? Notes);

