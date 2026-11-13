using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace LabTrack.Api.Controllers;

public sealed record WorkstationMaintenanceStatusDto(
    bool IsUnderMaintenance,
    string? Category,
    string? Description,
    string? Title,
    string? Since,
    decimal? Cost
);

public sealed record MaintenanceRecordDto(
    Guid Id,
    string Title,
    string Lab,
    string Category,
    decimal Cost,
    string Status,
    string? ScheduledDate,
    string Notes,
    DateTime CreatedAtUtc
);

public sealed record CreateMaintenanceRecordDto(
    string Title,
    string Lab,
    string Category,
    string Notes,
    decimal Cost,
    string? ScheduledDate,
    string Status
);

public sealed record UpdateMaintenanceRecordDto(
    string? Title,
    string? Lab,
    string? Category,
    string? Notes,
    decimal? Cost,
    string? ScheduledDate,
    string? Status
);

[ApiController]
[Route("api/v1/maintenance/records")]
[Authorize(Roles = "TECHNICIAN,ADMIN,ADMIN_TECHNICIAN")]
public sealed class MaintenanceRecordsController : ControllerBase
{
    private readonly LabTrackDbContext _db;
    private readonly ILogger<MaintenanceRecordsController> _logger;

    public MaintenanceRecordsController(LabTrackDbContext db, ILogger<MaintenanceRecordsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static void AddParam(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MaintenanceRecordDto>>>> GetRecords(CancellationToken ct)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ""Id"",
                       COALESCE(""Title"",'') AS ""Title"",
                       COALESCE(""WorkstationCodes"",'') AS ""Lab"",
                       COALESCE(""Category"",'') AS ""Category"",
                       COALESCE(""Cost"",0) AS ""Cost"",
                       COALESCE(""Status"",'PENDING') AS ""Status"",
                       CASE WHEN ""NextScheduledUtc"" IS NULL THEN NULL
                            ELSE TO_CHAR(""NextScheduledUtc"", 'YYYY-MM-DD') END AS ""ScheduledDate"",
                       COALESCE(""Description"",'') AS ""Notes"",
                       ""CreatedAtUtc""
                FROM labtrack_schema.""MaintenanceRecords""
                ORDER BY ""CreatedAtUtc"" DESC";

            var list = new List<MaintenanceRecordDto>();
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                list.Add(new MaintenanceRecordDto(
                    rd.GetGuid(0),
                    rd.IsDBNull(1) ? "" : rd.GetString(1),
                    rd.IsDBNull(2) ? "" : rd.GetString(2),
                    rd.IsDBNull(3) ? "" : rd.GetString(3),
                    rd.IsDBNull(4) ? 0m : rd.GetDecimal(4),
                    rd.IsDBNull(5) ? "PENDING" : rd.GetString(5),
                    rd.IsDBNull(6) ? null : rd.GetString(6),
                    rd.IsDBNull(7) ? "" : rd.GetString(7),
                    rd.GetDateTime(8)
                ));
            }
            return Ok(ApiResponse<List<MaintenanceRecordDto>>.Ok(list));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRecords failed");
            return StatusCode(500, ApiResponse<List<MaintenanceRecordDto>>.Fail("ERR", ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MaintenanceRecordDto>>> Create(
        [FromBody] CreateMaintenanceRecordDto req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Lab))
            return BadRequest(ApiResponse<MaintenanceRecordDto>.Fail("INVALID", "Title and PC Range required"));

        try
        {
            var id = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO labtrack_schema.""MaintenanceRecords""
                    (""Id"",""AssetId"",""MaintenanceType"",""Description"",""Cost"",""Status"",
                     ""PerformedAtUtc"",""NextScheduledUtc"",""CreatedAtUtc"",
                     ""WorkstationCodes"",""Category"",""Title"")
                    VALUES (@id, @assetId, @maintType, @desc, @cost, @status,
                            @perfAt, @nextSched, @createdAt,
                            @wsCodes, @cat, @title)";
                AddParam(cmd, "id", id);
                AddParam(cmd, "assetId", null);
                AddParam(cmd, "maintType", req.Category ?? "MAINTENANCE");
                AddParam(cmd, "desc", req.Notes ?? "");
                AddParam(cmd, "cost", req.Cost);
                AddParam(cmd, "status", req.Status ?? "PENDING");
                AddParam(cmd, "perfAt", now);
                AddParam(cmd, "nextSched", string.IsNullOrEmpty(req.ScheduledDate)
                    ? (object?)DBNull.Value
                    : DateTime.Parse(req.ScheduledDate + "T00:00:00Z"));
                AddParam(cmd, "createdAt", now);
                AddParam(cmd, "wsCodes", req.Lab);
                AddParam(cmd, "cat", req.Category ?? "");
                AddParam(cmd, "title", req.Title);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await SetWorkstationStatuses(req.Lab, "UNDER_MAINTENANCE", ct);

            return Ok(ApiResponse<MaintenanceRecordDto>.Ok(new MaintenanceRecordDto(
                id, req.Title, req.Lab, req.Category ?? "", req.Cost, req.Status ?? "PENDING",
                req.ScheduledDate, req.Notes ?? "", now)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed");
            return StatusCode(500, ApiResponse<MaintenanceRecordDto>.Fail("ERR", ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] UpdateMaintenanceRecordDto req, CancellationToken ct)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            string oldCodes = "";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT COALESCE(""WorkstationCodes"",'') FROM labtrack_schema.""MaintenanceRecords"" WHERE ""Id"" = @id";
                AddParam(cmd, "id", id);
                var o = await cmd.ExecuteScalarAsync(ct);
                if (o == null) return NotFound(ApiResponse<bool>.Fail("NOT_FOUND", "Record not found"));
                oldCodes = o.ToString() ?? "";
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE labtrack_schema.""MaintenanceRecords"" SET
                        ""Title"" = COALESCE(@title, ""Title""),
                        ""WorkstationCodes"" = COALESCE(@wsCodes, ""WorkstationCodes""),
                        ""Category"" = COALESCE(@cat, ""Category""),
                        ""Description"" = COALESCE(@desc, ""Description""),
                        ""Cost"" = COALESCE(@cost, ""Cost""),
                        ""Status"" = COALESCE(@status, ""Status""),
                        ""LastModifiedAtUtc"" = @now
                    WHERE ""Id"" = @id";
                AddParam(cmd, "id", id);
                AddParam(cmd, "title", req.Title);
                AddParam(cmd, "wsCodes", req.Lab);
                AddParam(cmd, "cat", req.Category);
                AddParam(cmd, "desc", req.Notes);
                AddParam(cmd, "cost", req.Cost);
                AddParam(cmd, "status", req.Status);
                AddParam(cmd, "now", DateTime.UtcNow);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            if (req.Status == "COMPLETED" && !string.IsNullOrEmpty(oldCodes))
                await SetWorkstationStatuses(oldCodes, "AVAILABLE", ct);

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed");
            return StatusCode(500, ApiResponse<bool>.Fail("ERR", ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            string oldCodes = "";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT COALESCE(""WorkstationCodes"",'') FROM labtrack_schema.""MaintenanceRecords"" WHERE ""Id"" = @id";
                AddParam(cmd, "id", id);
                var o = await cmd.ExecuteScalarAsync(ct);
                if (o == null) return NotFound(ApiResponse<bool>.Fail("NOT_FOUND", "Record not found"));
                oldCodes = o.ToString() ?? "";
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"DELETE FROM labtrack_schema.""MaintenanceRecords"" WHERE ""Id"" = @id";
                AddParam(cmd, "id", id);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            if (!string.IsNullOrEmpty(oldCodes))
                await SetWorkstationStatuses(oldCodes, "AVAILABLE", ct);
            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed");
            return StatusCode(500, ApiResponse<bool>.Fail("ERR", ex.Message));
        }
    }

    [HttpGet("/api/v1/maintenance/workstation-status/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<WorkstationMaintenanceStatusDto>>> GetWorkstationStatus(
        string code, CancellationToken ct)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT ""Status"" FROM labtrack_schema.""Workstation"" WHERE ""Code"" = @code LIMIT 1";
                AddParam(cmd, "code", code);
                var s = await cmd.ExecuteScalarAsync(ct);
                if (s == null)
                    return Ok(ApiResponse<WorkstationMaintenanceStatusDto>.Ok(
                        new WorkstationMaintenanceStatusDto(false, null, null, null, null, null)));

                var status = s.ToString() ?? "";
                if (!status.Equals("UNDER_MAINTENANCE", StringComparison.OrdinalIgnoreCase))
                    return Ok(ApiResponse<WorkstationMaintenanceStatusDto>.Ok(
                        new WorkstationMaintenanceStatusDto(false, null, null, null, null, null)));
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT ""Title"",""Category"",""Description"",""Cost"",""CreatedAtUtc"",""WorkstationCodes""
                    FROM labtrack_schema.""MaintenanceRecords""
                    WHERE ""WorkstationCodes"" IS NOT NULL
                    ORDER BY ""CreatedAtUtc"" DESC";

                var list = new List<(string Title, string Cat, string Desc, decimal Cost, DateTime Created, string Codes)>();
                using (var rd = await cmd.ExecuteReaderAsync(ct))
                {
                    while (await rd.ReadAsync(ct))
                    {
                        list.Add((
                            rd.IsDBNull(0) ? "" : rd.GetString(0),
                            rd.IsDBNull(1) ? "" : rd.GetString(1),
                            rd.IsDBNull(2) ? "" : rd.GetString(2),
                            rd.IsDBNull(3) ? 0m : rd.GetDecimal(3),
                            rd.GetDateTime(4),
                            rd.IsDBNull(5) ? "" : rd.GetString(5)
                        ));
                    }
                }

                foreach (var rec in list)
                {
                    var codes = ParseCodes(rec.Codes);
                    if (codes.Any(c => c.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    {
                        return Ok(ApiResponse<WorkstationMaintenanceStatusDto>.Ok(
                            new WorkstationMaintenanceStatusDto(
                                true, rec.Cat, rec.Desc, rec.Title,
                                rec.Created.ToString("yyyy-MM-dd"),
                                rec.Cost > 0 ? rec.Cost : (decimal?)null)));
                    }
                }

                return Ok(ApiResponse<WorkstationMaintenanceStatusDto>.Ok(
                    new WorkstationMaintenanceStatusDto(false, null, null, null, null, null)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetWorkstationStatus failed for {Code}", code);
            return StatusCode(500, ApiResponse<WorkstationMaintenanceStatusDto>.Fail("ERR", ex.Message));
        }
    }

    private async Task SetWorkstationStatuses(string codes, string newStatus, CancellationToken ct)
    {
        var list = ParseCodes(codes);
        if (list.Count == 0) return;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        var placeholders = string.Join(",", list.Select((_, i) => $"@c{i}"));
        cmd.CommandText = $@"UPDATE labtrack_schema.""Workstation"" 
            SET ""Status"" = @status, ""UpdatedAtUtc"" = @now 
            WHERE ""Code"" IN ({placeholders})";
        AddParam(cmd, "status", newStatus);
        AddParam(cmd, "now", DateTime.UtcNow);
        for (int i = 0; i < list.Count; i++)
            AddParam(cmd, $"c{i}", list[i]);
        try { await cmd.ExecuteNonQueryAsync(ct); } catch { }
    }

    private static List<string> ParseCodes(string input)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return result;

        // Split by comma or semicolon first â€” supports MULTIPLE ranges
        var parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var p = part.Trim();
            if (string.IsNullOrEmpty(p)) continue;
            
            // Check if this part is a range
            var m = Regex.Match(p,
                @"([A-Za-z]+-?)(\d+)\s*(?:to|-)\s*(?:[A-Za-z]+-?)(\d+)",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var prefix = m.Groups[1].Value;
                var start = int.Parse(m.Groups[2].Value);
                var end = int.Parse(m.Groups[3].Value);
                var padLen = m.Groups[2].Value.Length;
                // Safety: cap range at 500 to prevent accidental huge loops
                if (end - start > 500) end = start + 500;
                for (int i = start; i <= end; i++)
                    result.Add(prefix + i.ToString().PadLeft(padLen, '0'));
            }
            else
            {
                // Single code
                result.Add(p);
            }
        }
        return result;
    }
}

