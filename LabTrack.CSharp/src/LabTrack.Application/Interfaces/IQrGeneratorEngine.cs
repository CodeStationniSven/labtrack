namespace LabTrack.Application.Interfaces;

public interface IQrGeneratorEngine
{
    byte[] GeneratePng(
        string content, 
        int pixelsPerModule = 20);

    byte[] GenerateSvg(
        string content);

    string GenerateBase64Png(
        string content, 
        int pixelsPerModule = 20);
}