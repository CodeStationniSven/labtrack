using System.ComponentModel.DataAnnotations;
namespace LabTrack.Application.Dtos.Auth;
public class LoginRequestDto
{
    [Required] public string UsernameOrEmail { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
    public string? ExpectedRole { get; set; }
}