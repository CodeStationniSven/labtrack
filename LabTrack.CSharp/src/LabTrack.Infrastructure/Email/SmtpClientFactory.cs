using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace LabTrack.Infrastructure.Email;

public interface ISmtpClientFactory
{
    SmtpClient CreateClient();
}

public class SmtpClientFactory : ISmtpClientFactory
{
    private readonly EmailSettings _settings;

    public SmtpClientFactory(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public SmtpClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
        {
            throw new InvalidOperationException("SMTP server host is not configured in EmailSettings.");
        }

        var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        return client;
    }
}