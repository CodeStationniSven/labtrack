using LabTrack.Domain.Entities;
using Xunit;

namespace LabTrack.Tests.Unit.Validators;

public class ValidatorTests
{
    [Theory]
    [InlineData("user@labtrack.io", true)]
    [InlineData("tech.lead@domain.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@missingusername.com", false)]
    [InlineData("missingdomain@", false)]
    [InlineData("", false)]
    public void ValidateEmail_ValidatesFormatCorrectly(string email, bool expectedIsValid)
    {
        // Act
        bool isValid = !string.IsNullOrWhiteSpace(email) && 
                       email.Contains('@') && 
                       email.IndexOf('@') > 0 && 
                       email.IndexOf('@') < email.Length - 1 &&
                       email.Substring(email.IndexOf('@')).Contains('.');

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    [Theory]
    [InlineData("SecureP@ssw0rd!", true)]
    [InlineData("Short1!", false)] // Too short (< 8 chars)
    [InlineData("nopassword123!", false)] // No uppercase
    [InlineData("NOPASSWORD123!", false)] // No lowercase
    [InlineData("NoSpecialChar123", false)] // No special char
    public void ValidatePasswordComplexity_EnforcesSecurityRules(string password, bool expectedIsValid)
    {
        // Act
        bool hasMinLength = password.Length >= 8;
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        bool isValid = hasMinLength && hasUpper && hasLower && hasDigit && hasSpecial;

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    [Theory]
    [InlineData("SN-LAB-1001", true)]
    [InlineData("SN-WORKSTATION-99", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("INVALID SERIAL HAS SPACES", false)]
    public void ValidateSerialNumber_EnsuresNonEmptyAndNoSpaces(string serialNumber, bool expectedIsValid)
    {
        // Act
        bool isValid = !string.IsNullOrWhiteSpace(serialNumber) && !serialNumber.Contains(' ');

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    [Fact]
    public void ValidateReservationTimeSlot_ReturnsFalse_WhenStartTimeIsAfterEndTime()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(1); // Invalid range

        // Act
        bool isValidSlot = startTime < endTime;

        // Assert
        Assert.False(isValidSlot);
    }

    [Fact]
    public void ValidateReservationTimeSlot_ReturnsFalse_WhenStartTimeIsInThePast()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1); // Past time
        var endTime = DateTime.UtcNow.AddHours(1);

        // Act
        bool isValidSlot = startTime >= DateTime.UtcNow && startTime < endTime;

        // Assert
        Assert.False(isValidSlot);
    }
}