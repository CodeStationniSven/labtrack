using LabTrack.Application.Dtos.Auth;

namespace LabTrack.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(
        LoginRequestDto dto, 
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RegisterAsync(
        RegisterStaffDto dto, 
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto dto, 
        CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(
        string token, 
        CancellationToken cancellationToken = default);

    Task<bool> ValidateTokenAsync(
        string token, 
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId, 
        ChangePasswordDto dto, 
        CancellationToken cancellationToken = default);


    Task<LabTrack.Application.Dtos.Users.UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> VerifyPinAsync(VerifyPinDto dto, CancellationToken cancellationToken = default);
}


