using System.ComponentModel.DataAnnotations;
namespace LabTrack.Application.Dtos.Auth;
public class VerifyPinDto
{
    [Required] public string UsernameOrEmail { get; set; } = string.Empty;
    [Required] public string Pin { get; set; } = string.Empty;
}