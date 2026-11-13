using System.Security.Claims;
using LabTrack.Domain.Entities;
using LabTrack.Domain.Enums;

namespace LabTrack.Infrastructure.Authorization;

public interface IResourceAuthorizationService
{
    bool CanAccessAsset(ClaimsPrincipal user, Asset asset);
    bool CanManageReservation(ClaimsPrincipal user, Reservation reservation);
    bool CanViewTicket(ClaimsPrincipal user, RepairTicket ticket);
    bool CanModifyTicket(ClaimsPrincipal user, RepairTicket ticket);
}

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    public bool CanAccessAsset(ClaimsPrincipal user, Asset asset)
    {
        if (user is null || asset is null) return false;

        if (IsAdminTechnician(user)) return true;

        return asset.Status != AssetStatus.Decommissioned.ToString();
    }

    public bool CanManageReservation(ClaimsPrincipal user, Reservation reservation)
    {
        if (user is null || reservation is null) return false;

        if (IsAdminTechnician(user)) return true;

        var userId = GetUserId(user);
        if (userId is null) return false;

        return reservation.UserId == userId.Value;
    }

    public bool CanViewTicket(ClaimsPrincipal user, RepairTicket ticket)
    {
        if (user is null || ticket is null) return false;

        if (IsAdminTechnician(user)) return true;

        var userId = GetUserId(user);
        if (userId is null) return false;

        return ticket.ReportedByUserId == userId.Value
            || ticket.AssignedToUserId == userId.Value;
    }

    public bool CanModifyTicket(ClaimsPrincipal user, RepairTicket ticket)
    {
        if (user is null || ticket is null) return false;

        if (IsAdminTechnician(user)) return true;

        var userId = GetUserId(user);
        if (userId is null) return false;

        return ticket.AssignedToUserId == userId.Value
            || ticket.ReportedByUserId == userId.Value;
    }

    private static bool IsAdminTechnician(ClaimsPrincipal user)
    {
        return user.IsInRole(UserRole.AdminTechnician.ToString());
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }
}