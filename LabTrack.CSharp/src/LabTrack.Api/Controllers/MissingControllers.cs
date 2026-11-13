using LabTrack.Application.Interfaces;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace LabTrack.Api.Controllers;

[ApiController, Route("api/v1/maintenance-records"), Authorize]
public class MaintenanceRecordController : ControllerBase {
  private readonly LabTrackDbContext _d;
  public MaintenanceRecordController(LabTrackDbContext d) => _d = d;
  [HttpGet] public async Task<IActionResult> List([FromQuery]int page=1,[FromQuery]int pageSize=10) {
    var t = await _d.Set<MaintenanceRecord>().CountAsync();
    var i = await _d.Set<MaintenanceRecord>().AsNoTracking().OrderByDescending(x=>x.PerformedAtUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
    return Ok(new { success=true, data=new { items=i, totalCount=t, page, pageSize } });
  }
  [HttpPost] public async Task<IActionResult> Create([FromBody]MaintRecReq r) {
    var u = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if(!Guid.TryParse(u, out var uid)) return Unauthorized(new { success=false, error="No user id" });
    var x = new MaintenanceRecord { Id=Guid.NewGuid(), AssetId=r.AssetId, PerformedByUserId=uid, MaintenanceType=r.MaintenanceType??"General", Description=r.Description??"", PerformedAtUtc=DateTime.UtcNow, Cost=r.Cost, Status="Completed", CreatedAtUtc=DateTime.UtcNow };
    _d.Set<MaintenanceRecord>().Add(x); await _d.SaveChangesAsync();
    return Ok(new { success=true, data=x });
  }
}
public record MaintRecReq(Guid AssetId, string? MaintenanceType, string? Description, decimal? Cost);

[ApiController, Route("api/v1/replacement-records"), Authorize]
public class ReplacementRecordController : ControllerBase {
  private readonly LabTrackDbContext _d;
  public ReplacementRecordController(LabTrackDbContext d) => _d = d;
  [HttpGet] public async Task<IActionResult> List([FromQuery]int page=1,[FromQuery]int pageSize=10) {
    var t = await _d.Set<ReplacementRecord>().CountAsync();
    var i = await _d.Set<ReplacementRecord>().AsNoTracking().OrderByDescending(x=>x.RequestedAtUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
    return Ok(new { success=true, data=new { items=i, totalCount=t, page, pageSize } });
  }
  [HttpPost] public async Task<IActionResult> Create([FromBody]ReplRecReq r) {
    var u = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if(!Guid.TryParse(u, out var uid)) return Unauthorized(new { success=false });
    var x = new ReplacementRecord { Id=Guid.NewGuid(), LaboratoryId=r.LaboratoryId, WorkstationId=r.WorkstationId, OriginalAssetId=r.OriginalAssetId, ReplacementAssetId=r.ReplacementAssetId, ItemName=r.ItemName??"Item", Category=r.Category??"Peripheral", Quantity=r.Quantity<=0?1:r.Quantity, Urgency=r.Urgency??"Medium", Status=r.Status??"Pending", Justification=r.Justification??"", EstimatedCost=r.EstimatedCost, ApprovedCost=r.ApprovedCost, PreferredVendor=r.PreferredVendor, RequestedByUserId=uid, RequestedAtUtc=DateTime.UtcNow };
    _d.Set<ReplacementRecord>().Add(x); await _d.SaveChangesAsync();
    return Ok(new { success=true, data=x });
  }
}
public record ReplRecReq(Guid LaboratoryId, Guid? WorkstationId, Guid? OriginalAssetId, Guid? ReplacementAssetId, string? ItemName, string? Category, int Quantity, string? Urgency, string? Status, string? Justification, decimal? EstimatedCost, decimal? ApprovedCost, string? PreferredVendor);

[ApiController, Route("api/v1/network-incidents"), Authorize]
public class NetworkIncidentController : ControllerBase {
  private readonly LabTrackDbContext _d;
  public NetworkIncidentController(LabTrackDbContext d) => _d = d;
  [HttpGet] public async Task<IActionResult> List([FromQuery]int page=1,[FromQuery]int pageSize=10) {
    var t = await _d.Set<NetworkIncident>().CountAsync();
    var i = await _d.Set<NetworkIncident>().AsNoTracking().OrderByDescending(x=>x.IncidentDateUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
    return Ok(new { success=true, data=new { items=i, totalCount=t, page, pageSize } });
  }
  [HttpPost] public async Task<IActionResult> Create([FromBody]NetIncidentReq r) {
    var u = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    Guid? uid = Guid.TryParse(u, out var x) ? x : null;
    var e = new NetworkIncident { Id=Guid.NewGuid(), NetworkDeviceId=r.NetworkDeviceId, ReportedByUserId=uid, Title=r.Title??"Incident", Description=r.Description??"", Severity=r.Severity??"Medium", Status=r.Status??"Open", IncidentDateUtc=DateTime.UtcNow, CreatedAtUtc=DateTime.UtcNow };
    _d.Set<NetworkIncident>().Add(e); await _d.SaveChangesAsync();
    return Ok(new { success=true, data=e });
  }
  [HttpPatch("{id:guid}/resolve")] public async Task<IActionResult> Resolve(Guid id, [FromBody]ResolveReq r) {
    var e = await _d.Set<NetworkIncident>().FirstOrDefaultAsync(x=>x.Id==id);
    if(e==null) return NotFound(new { success=false });
    e.Status="Resolved"; e.ResolvedAtUtc=DateTime.UtcNow; e.ResolutionNotes=r.ResolutionNotes??"";
    await _d.SaveChangesAsync();
    return Ok(new { success=true, data=e });
  }
}
public record NetIncidentReq(Guid NetworkDeviceId, string? Title, string? Description, string? Severity, string? Status);
public record ResolveReq(string? ResolutionNotes);

[ApiController, Route("api/v1/instructor-communications"), Authorize]
public class InstructorCommunicationController : ControllerBase {
  private readonly LabTrackDbContext _d;
  public InstructorCommunicationController(LabTrackDbContext d) => _d = d;
  [HttpGet] public async Task<IActionResult> List([FromQuery]int page=1,[FromQuery]int pageSize=10) {
    var t = await _d.Set<InstructorCommunication>().CountAsync();
    var i = await _d.Set<InstructorCommunication>().AsNoTracking().OrderByDescending(x=>x.SentAtUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
    return Ok(new { success=true, data=new { items=i, totalCount=t, page, pageSize } });
  }
  [HttpPost] public async Task<IActionResult> Create([FromBody]InstCommReq r) {
    var u = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if(!Guid.TryParse(u, out var uid)) return Unauthorized(new { success=false });
    var x = new InstructorCommunication { Id=Guid.NewGuid(), InstructorUserId=r.InstructorUserId==Guid.Empty?uid:r.InstructorUserId, LaboratoryId=r.LaboratoryId, Subject=r.Subject??"Message", Message=r.Message??"", Priority=r.Priority??"Normal", SentAtUtc=DateTime.UtcNow, IsRead=false, CreatedAtUtc=DateTime.UtcNow };
    _d.Set<InstructorCommunication>().Add(x); await _d.SaveChangesAsync();
    return Ok(new { success=true, data=x });
  }
}
public record InstCommReq(Guid InstructorUserId, Guid LaboratoryId, string? Subject, string? Message, string? Priority);


