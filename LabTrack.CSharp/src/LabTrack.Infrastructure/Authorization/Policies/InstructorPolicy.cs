using LabTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LabTrack.Infrastructure.Authorization.Policies;

public static class InstructorPolicy
{
    public const string PolicyName = "InstructorOnly";

    public static void BuildPolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireAuthenticatedUser()
              .RequireRole(UserRole.Instructor.ToString());
    }
}