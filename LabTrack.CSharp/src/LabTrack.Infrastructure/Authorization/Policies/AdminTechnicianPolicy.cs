using LabTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LabTrack.Infrastructure.Authorization.Policies;

public static class AdminTechnicianPolicy
{
    public const string PolicyName = "AdminTechnicianOnly";

    public static void BuildPolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireAuthenticatedUser()
              .RequireRole(UserRole.AdminTechnician.ToString());
    }
}