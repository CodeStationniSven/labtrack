using System.Text;
using LabTrack.Application.Interfaces;
using QRCoder;

namespace LabTrack.Infrastructure.Qr;

public class QrGeneratorEngine : IQrGeneratorEngine
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }

    public byte[] GenerateSvg(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new SvgQRCode(qrCodeData);

        var svgXml = qrCode.GetGraphic(20);
        return Encoding.UTF8.GetBytes(svgXml);
    }

    public string GenerateBase64Png(string payload, int pixelsPerModule = 20)
    {
        var pngBytes = GeneratePng(payload, pixelsPerModule);
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }
}