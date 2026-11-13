using LabTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LabTrack.Infrastructure.Authorization.Policies;

public static class StudentPolicy
{
    public const string PolicyName = "StudentOnly";

    public static void BuildPolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireAuthenticatedUser()
              .RequireRole(UserRole.Student.ToString());
    }
}