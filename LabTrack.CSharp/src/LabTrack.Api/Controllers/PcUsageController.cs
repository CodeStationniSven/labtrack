using LabTrack.Application.Interfaces;
using LabTrack.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

public sealed record ScanLoginRequestDto(
    string QrToken,
    string StudentNumber,
    string PinCode
);

public sealed record HeartbeatRequestDto(
    Guid SessionId,
    string QrToken
);

public sealed record PcSessionDto(
    Guid SessionId,
    Guid WorkstationId,
    string WorkstationNumber,
    string LabName,
    Guid StudentUserId,
    string StudentName,
    DateTime StartTime,
    DateTime? EndTime,
    DateTime LastHeartbeatAt,
    string Status
);

[ApiController]
[Route("api/v1/pc-usage")]
public sealed class PcUsageController : ControllerBase
{
    private readonly IPcUsageService _pcUsageService;
    private readonly ILogger<PcUsageController> _logger;

    public PcUsageController(
        IPcUsageService pcUsageService,
        ILogger<PcUsageController> logger)
    {
        _pcUsageService = pcUsageService;
        _logger = logger;
    }

    [HttpPost("scan-login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PcSessionDto>>> ScanLogin(
        [FromBody] ScanLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.QrToken) || string.IsNullOrWhiteSpace(request.StudentNumber) || string.IsNullOrWhiteSpace(request.PinCode))
        {
            return BadRequest(ApiResponse<PcSessionDto>.Fail(
                "INVALID_SCAN_DATA",
                "QR Token, Student Number, and PIN Code are required."));
        }

        try
        {
            var session = await _pcUsageService.ScanLoginAsync(request, cancellationToken);
            if (session is null)
            {
                return Unauthorized(ApiResponse<PcSessionDto>.Fail(
                    "SCAN_LOGIN_FAILED",
                    "Invalid QR code, student credentials, or workstation is unavailable."));
            }

            return Ok(ApiResponse<PcSessionDto>.Ok(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing QR scan login for student {StudentNumber}", request.StudentNumber);
            return StatusCode(500, ApiResponse<PcSessionDto>.Fail(
                "PC_USAGE_ERROR",
                "An error occurred while initiating PC session."));
        }
    }

    [HttpPost("heartbeat")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> Heartbeat(
        [FromBody] HeartbeatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(request.QrToken))
        {
            return BadRequest(ApiResponse<bool>.Fail(
                "INVALID_HEARTBEAT_DATA",
                "Session ID and QR Token are required."));
        }

        try
        {
            var success = await _pcUsageService.HeartbeatAsync(request, cancellationToken);
            if (!success)
            {
                return BadRequest(ApiResponse<bool>.Fail(
                    "HEARTBEAT_FAILED",
                    "Session is inactive, expired, or QR token validation failed."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating heartbeat for session ID {SessionId}", request.SessionId);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "PC_USAGE_ERROR",
                "An error occurred while processing heartbeat signal."));
        }
    }

    [HttpPost("end-session/{sessionId:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> EndSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var ended = await _pcUsageService.EndSessionAsync(sessionId, cancellationToken);
            if (!ended)
            {
                return NotFound(ApiResponse<bool>.Fail(
                    "SESSION_NOT_FOUND",
                    $"Active session with ID '{sessionId}' was not found."));
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending PC session ID {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<bool>.Fail(
                "PC_USAGE_ERROR",
                "An error occurred while ending the PC session."));
        }
    }

    [HttpGet("active")]
    [Authorize(Roles = "TECHNICIAN,INSTRUCTOR,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<PcSessionDto>>>> GetActiveSessions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? labId = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<PcSessionDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        try
        {
            var result = await _pcUsageService.GetActiveSessionsAsync(pageNumber, pageSize, labId, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<PcSessionDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active PC usage sessions");
            return StatusCode(500, ApiResponse<PaginationResponse<PcSessionDto>>.Fail(
                "PC_USAGE_ERROR",
                "An error occurred while fetching active sessions."));
        }
    }

    [HttpGet("history")]
    [Authorize(Roles = "TECHNICIAN,INSTRUCTOR,ADMIN,ADMIN_TECHNICIAN")]
    public async Task<ActionResult<ApiResponse<PaginationResponse<PcSessionDto>>>> GetSessionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest(ApiResponse<PaginationResponse<PcSessionDto>>.Fail(
                "INVALID_PAGINATION",
                "Page number and page size must be greater than zero."));
        }

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            return BadRequest(ApiResponse<PaginationResponse<PcSessionDto>>.Fail(
                "INVALID_DATE_RANGE",
                "Start date cannot be later than end date."));
        }

        try
        {
            var result = await _pcUsageService.GetSessionHistoryAsync(pageNumber, pageSize, userId, startDate, endDate, cancellationToken);
            return Ok(ApiResponse<PaginationResponse<PcSessionDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PC session history");
            return StatusCode(500, ApiResponse<PaginationResponse<PcSessionDto>>.Fail(
                "PC_USAGE_ERROR",
                "An error occurred while fetching session history."));
        }
    }

    // ================================================================
    // AUTO-ASSIGN scan-login â€” finds first Available PC in laboratory
    // ================================================================
    public sealed record AutoScanLoginRequest(
        Guid LaboratoryId,
        string? LaboratoryName = null,
        string? PreferredWorkstationCode = null,
        string? IpAddress = null,
        string? UserAgent = null
    );

    public sealed record AutoScanLoginResponse(
        Guid SessionId,
        string AssignedPc,
        string LaboratoryName,
        DateTime SessionStartUtc,
        int RemainingAvailable
    );

    [HttpPost("scan-login-auto")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AutoScanLoginResponse>>> ScanLoginAuto(
        [FromBody] AutoScanLoginRequest request, CancellationToken ct)
    {
        try
        {
            // Get current user from JWT
            var user = HttpContext.User;
            var userIdClaim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? user?.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId) || userId == Guid.Empty)
                return Unauthorized(ApiResponse<AutoScanLoginResponse>.Fail("UNAUTHORIZED", "User context not found"));

            var _dbCtx = HttpContext.RequestServices.GetRequiredService<LabTrack.Infrastructure.Persistence.DbContext.LabTrackDbContext>();
            var conn = _dbCtx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);

            // Resolve laboratory â€” by ID or by Name
            Guid resolvedLabId = request.LaboratoryId;
            string labName = "";
            
            if (resolvedLabId != Guid.Empty)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ""Name"" FROM labtrack_schema.""Laboratories"" WHERE ""Id"" = @labId";
                    var p = cmd.CreateParameter(); p.ParameterName = "labId"; p.Value = resolvedLabId; cmd.Parameters.Add(p);
                    var r = await cmd.ExecuteScalarAsync(ct);
                    if (r != null) labName = r.ToString() ?? "";
                }
            }
            
            // Fallback: lookup by name if ID empty or not found
            if (string.IsNullOrEmpty(labName) && !string.IsNullOrEmpty(request.LaboratoryName))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ""Id"", ""Name"" FROM labtrack_schema.""Laboratories"" 
                        WHERE LOWER(""Name"") = LOWER(@name) OR LOWER(""Name"") LIKE LOWER(@partial) LIMIT 1";
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "name"; p1.Value = request.LaboratoryName; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "partial"; p2.Value = "%" + request.LaboratoryName + "%"; cmd.Parameters.Add(p2);
                    using var rd = await cmd.ExecuteReaderAsync(ct);
                    if (await rd.ReadAsync(ct))
                    {
                        resolvedLabId = rd.GetGuid(0);
                        labName = rd.GetString(1);
                    }
                }
            }
            
            // Final fallback: first active lab
            if (resolvedLabId == Guid.Empty)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ""Id"", ""Name"" FROM labtrack_schema.""Laboratories"" 
                        WHERE COALESCE(""IsActive"", true) = true ORDER BY ""Name"" LIMIT 1";
                    using var rd = await cmd.ExecuteReaderAsync(ct);
                    if (await rd.ReadAsync(ct))
                    {
                        resolvedLabId = rd.GetGuid(0);
                        labName = rd.GetString(1);
                    }
                }
            }
            
            // ULTIMATE FALLBACK: first lab with available workstations
            if (resolvedLabId == Guid.Empty)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT l.""Id"", l.""Name"" FROM labtrack_schema.""Laboratories"" l
                        WHERE EXISTS (SELECT 1 FROM labtrack_schema.""Workstation"" w 
                                      WHERE w.""LaboratoryId"" = l.""Id"" 
                                      AND LOWER(w.""Status"") IN ('available', 'operational'))
                        ORDER BY l.""Name"" LIMIT 1";
                    using var rd = await cmd.ExecuteReaderAsync(ct);
                    if (await rd.ReadAsync(ct))
                    {
                        resolvedLabId = rd.GetGuid(0);
                        labName = rd.GetString(1);
                    }
                }
            }
            
            // FINAL FALLBACK: any lab at all
            if (resolvedLabId == Guid.Empty)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ""Id"", ""Name"" FROM labtrack_schema.""Laboratories"" ORDER BY ""Name"" LIMIT 1";
                    using var rd = await cmd.ExecuteReaderAsync(ct);
                    if (await rd.ReadAsync(ct))
                    {
                        resolvedLabId = rd.GetGuid(0);
                        labName = rd.GetString(1);
                    }
                }
            }
            
            if (resolvedLabId == Guid.Empty)
                return NotFound(ApiResponse<AutoScanLoginResponse>.Fail("LAB_NOT_FOUND", "No laboratory found in system"));

            // If user already has active session â€” return it
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT p.""Id"", w.""Code"", p.""SessionStartUtc""
                    FROM labtrack_schema.""PcUsageLogs"" p
                    JOIN labtrack_schema.""Workstation"" w ON w.""Id"" = p.""WorkstationId""
                    WHERE p.""UserId"" = @uid AND p.""SessionEndUtc"" IS NULL
                    ORDER BY p.""SessionStartUtc"" DESC LIMIT 1";
                var p = cmd.CreateParameter(); p.ParameterName = "uid"; p.Value = userId; cmd.Parameters.Add(p);
                using var rd = await cmd.ExecuteReaderAsync(ct);
                if (await rd.ReadAsync(ct))
                {
                    return Ok(ApiResponse<AutoScanLoginResponse>.Ok(new AutoScanLoginResponse(
                        rd.GetGuid(0), rd.GetString(1), labName, rd.GetDateTime(2), 0)));
                }
            }

            // Count available workstations
            int availableCount = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT COUNT(*) FROM labtrack_schema.""Workstation""
                    WHERE ""LaboratoryId"" = @labId 
                    AND LOWER(""Status"") IN ('available', 'operational')
                    ";
                var p = cmd.CreateParameter(); p.ParameterName = "labId"; p.Value = resolvedLabId; cmd.Parameters.Add(p);
                availableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct) ?? 0);
            }

            if (availableCount == 0)
            {
                return Conflict(ApiResponse<AutoScanLoginResponse>.Fail(
                    "ALL_PCS_OCCUPIED",
                    $"No available PCs in {labName}. Total workstations: {availableCount}"));
            }

            // Find first available workstation (prefer requested if available)
            Guid assignedWsId = Guid.Empty;
            string assignedCode = "";
            using (var cmd = conn.CreateCommand())
            {
                if (!string.IsNullOrEmpty(request.PreferredWorkstationCode))
                {
                    cmd.CommandText = @"
                        SELECT ""Id"", ""Code"" FROM labtrack_schema.""Workstation""
                        WHERE ""LaboratoryId"" = @labId 
                        AND ""Code"" = @code
                        AND LOWER(""Status"") IN ('available', 'operational')
                         LIMIT 1";
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "labId"; p1.Value = request.LaboratoryId; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "code"; p2.Value = request.PreferredWorkstationCode; cmd.Parameters.Add(p2);
                    using var rd = await cmd.ExecuteReaderAsync(ct);
                    if (await rd.ReadAsync(ct))
                    {
                        assignedWsId = rd.GetGuid(0);
                        assignedCode = rd.GetString(1);
                    }
                    cmd.Parameters.Clear();
                }

                if (assignedWsId == Guid.Empty)
                {
                    cmd.CommandText = @"
                        SELECT ""Id"", ""Code"" FROM labtrack_schema.""Workstation""
                        WHERE ""LaboratoryId"" = @labId 
                        AND LOWER(""Status"") IN ('available', 'operational')
                        
                        AND ""Code"" IS NOT NULL AND ""Code"" <> ''
                        ORDER BY ""Code"" ASC LIMIT 1";
                    var p = cmd.CreateParameter(); p.ParameterName = "labId"; p.Value = resolvedLabId; cmd.Parameters.Add(p);
                    using var rd = await cmd.ExecuteReaderAsync(ct);
                    if (await rd.ReadAsync(ct))
                    {
                        assignedWsId = rd.GetGuid(0);
                        assignedCode = rd.GetString(1);
                    }
                }
            }

            if (assignedWsId == Guid.Empty)
            {
                var reason = $"No assignable PC in {labName}. Available count: {availableCount}. Check Workstation.Code and IsActive.";
                return Conflict(ApiResponse<AutoScanLoginResponse>.Fail("ALL_PCS_OCCUPIED", reason));
            }

            // Mark workstation In Use
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE labtrack_schema.""Workstation"" 
                    SET ""Status"" = 'In Use', ""LastSeenAtUtc"" = @now, ""UpdatedAtUtc"" = @now 
                    WHERE ""Id"" = @id";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "now"; p1.Value = DateTime.UtcNow; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "id"; p2.Value = assignedWsId; cmd.Parameters.Add(p2);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Create PcUsageLogs
            var sessionId = Guid.NewGuid();
            var startUtc = DateTime.UtcNow;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO labtrack_schema.""PcUsageLogs"" 
                    (""Id"", ""WorkstationId"", ""UserId"", ""SessionStartUtc"", ""Status"")
                    VALUES (@id, @wsId, @uid, @start, 'ACTIVE')";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "id"; p1.Value = sessionId; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "wsId"; p2.Value = assignedWsId; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.ParameterName = "uid"; p3.Value = userId; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.ParameterName = "start"; p4.Value = startUtc; cmd.Parameters.Add(p4);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            return Ok(ApiResponse<AutoScanLoginResponse>.Ok(new AutoScanLoginResponse(
                sessionId, assignedCode, labName, startUtc, availableCount - 1)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScanLoginAuto failed");
            return StatusCode(500, ApiResponse<AutoScanLoginResponse>.Fail("ERR", ex.Message));
        }
    }

    // ================================================================
    // FIXED end-session â€” releases workstation back to Available
    // ================================================================
    [HttpPost("end-session-release/{sessionId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> EndSessionRelease(Guid sessionId, CancellationToken ct)
    {
        try
        {
            var _dbCtx = HttpContext.RequestServices.GetRequiredService<LabTrack.Infrastructure.Persistence.DbContext.LabTrackDbContext>();
            var conn = _dbCtx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);

            Guid wsId = Guid.Empty;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT ""WorkstationId"" FROM labtrack_schema.""PcUsageLogs"" 
                    WHERE ""Id"" = @id AND ""SessionEndUtc"" IS NULL";
                var p = cmd.CreateParameter(); p.ParameterName = "id"; p.Value = sessionId; cmd.Parameters.Add(p);
                var r = await cmd.ExecuteScalarAsync(ct);
                if (r == null) return NotFound(ApiResponse<bool>.Fail("NOT_FOUND", "Active session not found"));
                wsId = (Guid)r;
            }

            var now = DateTime.UtcNow;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE labtrack_schema.""PcUsageLogs"" 
                    SET ""SessionEndUtc"" = @now, ""Status"" = 'ENDED' WHERE ""Id"" = @id";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "now"; p1.Value = now; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "id"; p2.Value = sessionId; cmd.Parameters.Add(p2);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE labtrack_schema.""Workstation"" 
                    SET ""Status"" = 'Available', ""UpdatedAtUtc"" = @now WHERE ""Id"" = @id";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "now"; p1.Value = now; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "id"; p2.Value = wsId; cmd.Parameters.Add(p2);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            return Ok(ApiResponse<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EndSessionRelease failed");
            return StatusCode(500, ApiResponse<bool>.Fail("ERR", ex.Message));
        }
    }
}

