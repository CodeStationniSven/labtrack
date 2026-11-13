using LabTrack.Application.Interfaces;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Api.Controllers;

[ApiController, Route("api/v1/idempotency-records"), Authorize]
public class IdempotencyRecordsController : ControllerBase
{
    private readonly LabTrackDbContext _d;
    public IdempotencyRecordsController(LabTrackDbContext d) => _d = d;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        
        var rows = await _d.Database
            .SqlQueryRaw<IdempotencyRow>(
                "SELECT \"Key\", \"ResponseJson\", \"CreatedAt\" FROM labtrack_schema.\"IdempotencyRecords\" ORDER BY \"CreatedAt\" DESC LIMIT {0} OFFSET {1}",
                pageSize, (page - 1) * pageSize)
            .ToListAsync();
        
        var total = await _d.Database
            .SqlQueryRaw<CountRow>("SELECT COUNT(*)::int AS \"Value\" FROM labtrack_schema.\"IdempotencyRecords\"")
            .FirstOrDefaultAsync();
        
        return Ok(new { 
            success = true, 
            data = new { 
                items = rows, 
                totalCount = total?.Value ?? 0, 
                page, 
                pageSize 
            } 
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        try
        {
            var key = "test-" + Guid.NewGuid().ToString("N");
            var json = "{\"success\":true,\"key\":\"" + key + "\"}";
            
            await _d.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO labtrack_schema.""IdempotencyRecords"" 
                (""Key"", ""ResponseJson"", ""CreatedAt"")
                VALUES ({key}, {json}, NOW())
            ");
            return Ok(new { success = true, key });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}

public class IdempotencyRow
{
    public string Key { get; set; } = "";
    public string? ResponseJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CountRow
{
    public int Value { get; set; }
}

