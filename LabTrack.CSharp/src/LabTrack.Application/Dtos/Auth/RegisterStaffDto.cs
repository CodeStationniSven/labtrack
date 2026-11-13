using System.ComponentModel.DataAnnotations;

namespace LabTrack.Application.Dtos.Auth;

public class RegisterStaffDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "STUDENT";
    public string? Campus { get; set; }
    public string? Pin { get; set; }
}