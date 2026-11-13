using System.Security.Claims;
using Xunit;

namespace LabTrack.Tests.Security;

public class AuthorizationTests
{
    private enum Role
    {
        Student,
        Technician,
        Administrator
    }

    private class SecurityContext
    {
        public ClaimsPrincipal User { get; set; } = new(new ClaimsIdentity());
    }

    [Fact]
    public void Authorize_UserWithRequiredRole_GrantsAccess()
    {
        // Arrange
        var userClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Role.Administrator.ToString())
        };
        var identity = new ClaimsIdentity(userClaims, "TestAuth");
        var context = new SecurityContext { User = new ClaimsPrincipal(identity) };

        // Act
        var hasAccess = HasRequiredRole(context.User, Role.Administrator);

        // Assert
        Assert.True(hasAccess);
    }

    [Fact]
    public void Authorize_UserLackingRequiredRole_DeniesAccess()
    {
        // Arrange
        var userClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Role.Student.ToString())
        };
        var identity = new ClaimsIdentity(userClaims, "TestAuth");
        var context = new SecurityContext { User = new ClaimsPrincipal(identity) };

        // Act
        var hasAccess = HasRequiredRole(context.User, Role.Administrator);

        // Assert
        Assert.False(hasAccess);
    }

    [Fact]
    public void Authorize_UserWithMultipleRoles_GrantsAccessIfAnyMatch()
    {
        // Arrange
        var userClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Role.Student.ToString()),
            new Claim(ClaimTypes.Role, Role.Technician.ToString())
        };
        var identity = new ClaimsIdentity(userClaims, "TestAuth");
        var context = new SecurityContext { User = new ClaimsPrincipal(identity) };

        // Act
        var hasAccess = HasAnyRole(context.User, Role.Technician, Role.Administrator);

        // Assert
        Assert.True(hasAccess);
    }

    [Fact]
    public void Authorize_UnauthenticatedUser_DeniesAccess()
    {
        // Arrange
        var context = new SecurityContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };

        // Act
        var hasAccess = HasRequiredRole(context.User, Role.Student);

        // Assert
        Assert.False(hasAccess);
    }

    private static bool HasRequiredRole(ClaimsPrincipal principal, Role requiredRole)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return false;

        return principal.IsInRole(requiredRole.ToString());
    }

    private static bool HasAnyRole(ClaimsPrincipal principal, params Role[] allowedRoles)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return false;

        return allowedRoles.Any(role => principal.IsInRole(role.ToString()));
    }
}