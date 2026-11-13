using LabTrack.Application.Dtos.Auth;
using LabTrack.Application.Dtos.Users;
using LabTrack.Application.Interfaces;
using LabTrack.Domain.Entities;

namespace LabTrack.Application.Services;

public class AuthService : IAuthService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IUnitOfWork unitOfWork)
    {
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

        public async Task<AuthResponseDto> VerifyPinAsync(VerifyPinDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) || string.IsNullOrWhiteSpace(dto.Pin))
            throw new ArgumentException("Username/Email and PIN are required.");

        var user = await _unitOfWork.Users.GetByEmailAsync(dto.UsernameOrEmail, cancellationToken);
        if (user is null) throw new UnauthorizedAccessException("Invalid credentials.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Account is inactive.");
        
        // First-time PIN setup for old accounts (NULL/empty PIN)
            if (string.IsNullOrEmpty(user.Pin))
            {
                // Save the PIN they entered as their PIN
                user.Pin = dto.Pin;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else if (user.Pin != dto.Pin)
            {
                throw new UnauthorizedAccessException("Invalid PIN.");
            }

        user.LastLoginUtc = DateTime.UtcNow;

        var roles = new List<string> { user.Role };
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var accessExpires = DateTime.UtcNow.AddHours(1);
        var refreshExpires = DateTime.UtcNow.AddDays(7);

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshToken,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _unitOfWork.AuthSessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = accessExpires,
            AccessTokenExpiresAtUtc = accessExpires,
            RefreshTokenExpiresAtUtc = refreshExpires,
            User = MapUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Username/Email and password are required.");

        var user = await _unitOfWork.Users.GetByEmailAsync(dto.UsernameOrEmail, cancellationToken);
        if (user is null) throw new UnauthorizedAccessException("Invalid credentials.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Account is inactive.");
        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginUtc = DateTime.UtcNow;

        var roles = new List<string> { user.Role };
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var accessExpires = DateTime.UtcNow.AddHours(1);
        var refreshExpires = DateTime.UtcNow.AddDays(7);

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshToken,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _unitOfWork.AuthSessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = accessExpires,
            AccessTokenExpiresAtUtc = accessExpires,
            RefreshTokenExpiresAtUtc = refreshExpires,
            User = MapUserDto(user)
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterStaffDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var existing = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken);
        if (existing is not null) throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
                Pin = dto.Pin,
            Role = dto.Role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = new List<string> { user.Role };
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var accessExpires = DateTime.UtcNow.AddHours(1);
        var refreshExpires = DateTime.UtcNow.AddDays(7);

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshToken,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _unitOfWork.AuthSessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = accessExpires,
            AccessTokenExpiresAtUtc = accessExpires,
            RefreshTokenExpiresAtUtc = refreshExpires,
            User = MapUserDto(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            throw new ArgumentException("Refresh token must be provided.", nameof(dto.RefreshToken));

        var session = await _unitOfWork.AuthSessions.GetByRefreshTokenAsync(dto.RefreshToken, cancellationToken);
        if (session is null || !session.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        var roles = new List<string> { user.Role };
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var accessExpires = DateTime.UtcNow.AddHours(1);
        var refreshExpires = DateTime.UtcNow.AddDays(7);

        session.RevokedAtUtc = DateTime.UtcNow;
        session.ReplacedByToken = newRefreshToken;
        await _unitOfWork.AuthSessions.UpdateAsync(session, cancellationToken);

        var newSession = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = newRefreshToken,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _unitOfWork.AuthSessions.AddAsync(newSession, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAtUtc = accessExpires,
            AccessTokenExpiresAtUtc = accessExpires,
            RefreshTokenExpiresAtUtc = refreshExpires,
            User = MapUserDto(user)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be empty.", nameof(refreshToken));

        var session = await _unitOfWork.AuthSessions.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (session is not null && session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = DateTime.UtcNow;
            await _unitOfWork.AuthSessions.UpdateAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(token));

    public async Task ChangePasswordAsync(Guid userId, LabTrack.Application.Dtos.Auth.ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentNullException.ThrowIfNull(dto);
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new InvalidOperationException("User not found.");
        if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");
        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
        user.LastModifiedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : MapUserDto(user);
    }

    private static UserDto MapUserDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Email,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role,
        IsActive = user.IsActive
    };
}

