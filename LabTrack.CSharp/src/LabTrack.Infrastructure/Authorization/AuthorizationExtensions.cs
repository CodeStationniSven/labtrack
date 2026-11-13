using LabTrack.Infrastructure.Authorization.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace LabTrack.Infrastructure.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminTechnicianPolicy.PolicyName, AdminTechnicianPolicy.BuildPolicy);
            options.AddPolicy(InstructorPolicy.PolicyName, InstructorPolicy.BuildPolicy);
            options.AddPolicy(StudentPolicy.PolicyName, StudentPolicy.BuildPolicy);
        });

        return services;
    }
}