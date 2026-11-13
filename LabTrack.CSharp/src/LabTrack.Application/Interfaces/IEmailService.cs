namespace LabTrack.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        bool isHtml = true, 
        CancellationToken cancellationToken = default);

    Task SendTemplatedEmailAsync<TModel>(
        string to, 
        string templateName, 
        TModel model, 
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        string to, 
        string resetToken, 
        CancellationToken cancellationToken = default);

    Task SendWelcomeEmailAsync(
        string to, 
        string userName, 
        CancellationToken cancellationToken = default);
}