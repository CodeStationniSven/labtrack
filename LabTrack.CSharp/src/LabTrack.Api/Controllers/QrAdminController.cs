using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Api.Controllers;

[ApiController]
[Route("api/v1/admin/qr")]
[Authorize(Roles = "ADMIN,ADMIN_TECHNICIAN,SUPER_ADMIN,LAB_ADMIN")]
public sealed class QrAdminController : ControllerBase
{
    private readonly LabTrackDbContext _db;
    private readonly ILogger<QrAdminController> _logger;

    public QrAdminController(LabTrackDbContext db, ILogger<QrAdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 500) pageSize = 100;
            var q = _db.QrCodes.AsNoTracking();
            var total = await q.CountAsync(cancellationToken);
            var items = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { items, total, pageNumber, pageSize }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing QR codes");
            return StatusCode(500, ApiResponse<object>.Fail("QR_LIST_ERROR", ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var qr = await _db.QrCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (qr is null)
                return NotFound(ApiResponse<bool>.Fail("QR_NOT_FOUND", $"QR code {id} not found"));
            qr.IsDeleted = true;
            qr.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting QR {QrId}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("QR_DELETE_ERROR", ex.Message));
        }
    }
}
