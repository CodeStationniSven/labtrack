namespace LabTrack.Application.Interfaces;

public interface IQrTokenService
{
    string GenerateToken(string entityType, Guid entityId);
    bool ValidateToken(string token);
}