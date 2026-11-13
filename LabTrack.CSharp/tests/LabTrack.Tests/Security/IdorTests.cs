using System.Security.Claims;
using Xunit;

namespace LabTrack.Tests.Security;

public class IdorTests
{
    private class SecurityContext
    {
        public ClaimsPrincipal User { get; set; } = new(new ClaimsIdentity());
    }

    private class Resource
    {
        public Guid Id { get; set; }
        public Guid OwnerUserId { get; set; }
    }

    [Fact]
    public void AccessResource_WhenUserIsResourceOwner_GrantsAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId
        };

        var context = CreateSecurityContext(userId, "Student");

        // Act
        var canAccess = CanAccessResource(context.User, resource);

        // Assert
        Assert.True(canAccess);
    }

    [Fact]
    public void AccessResource_WhenUserIsNotOwnerAndNotAdmin_DeniesAccess()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        var context = CreateSecurityContext(attackerUserId, "Student");

        // Act
        var canAccess = CanAccessResource(context.User, resource);

        // Assert
        Assert.False(canAccess);
    }

    [Fact]
    public void AccessResource_WhenUserIsAdmin_GrantsAccessToAnyResource()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        var context = CreateSecurityContext(adminUserId, "Administrator");

        // Act
        var canAccess = CanAccessResource(context.User, resource);

        // Assert
        Assert.True(canAccess);
    }

    [Fact]
    public void AccessResource_WhenUserIsTechnician_GrantsAccessToAnyResource()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var techUserId = Guid.NewGuid();

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId
        };

        var context = CreateSecurityContext(techUserId, "Technician");

        // Act
        var canAccess = CanAccessResource(context.User, resource);

        // Assert
        Assert.True(canAccess);
    }

    private static SecurityContext CreateSecurityContext(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new SecurityContext { User = new ClaimsPrincipal(identity) };
    }

    private static bool CanAccessResource(ClaimsPrincipal principal, Resource resource)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return false;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
            return false;

        // Elevated roles bypass ownership checks
        if (principal.IsInRole("Administrator") || principal.IsInRole("Technician"))
            return true;

        // Enforce Direct Object Reference ownership constraint
        return resource.OwnerUserId == currentUserId;
    }
}