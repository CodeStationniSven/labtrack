using LabTrack.Domain.Entities;

namespace LabTrack.Infrastructure.Email;

public interface IEmailTemplateService
{
    string GetReservationConfirmationTemplate(User user, Asset asset, DateTime startDate, DateTime endDate);
    string GetTicketStatusUpdateTemplate(User user, RepairTicket ticket, string previousStatus);
    string GetPasswordResetTemplate(User user, string resetToken, string resetUrl);
}

public class EmailTemplateService : IEmailTemplateService
{
    public string GetReservationConfirmationTemplate(User user, Asset asset, DateTime startDate, DateTime endDate)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; }
                    .header { background-color: #0056b3; color: #ffffff; padding: 15px; text-align: center; border-radius: 8px 8px 0 0; }
                    .content { padding: 20px; }
                    .details { background-color: #f9f9f9; padding: 15px; border-left: 4px solid #0056b3; margin: 15px 0; }
                    .footer { font-size: 0.8em; color: #777; text-align: center; margin-top: 20px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h2>Reservation Confirmed</h2>
                    </div>
                    <div class="content">
                        <p>Hello {{user.FirstName}},</p>
                        <p>Your asset reservation request has been successfully processed.</p>
                        <div class="details">
                            <p><strong>Asset:</strong> {{asset.Name}} ({{asset.AssetTag}})</p>
                            <p><strong>Start Date:</strong> {{startDate:f}}</p>
                            <p><strong>End Date:</strong> {{endDate:f}}</p>
                        </div>
                        <p>Please ensure you collect the asset on time and report any pre-existing damage prior to use.</p>
                    </div>
                    <div class="footer">
                        <p>This is an automated notification from the LabTrack System. Please do not reply directly to this email.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public string GetTicketStatusUpdateTemplate(User user, RepairTicket ticket, string previousStatus)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; }
                    .header { background-color: #28a745; color: #ffffff; padding: 15px; text-align: center; border-radius: 8px 8px 0 0; }
                    .content { padding: 20px; }
                    .status-box { background-color: #f4f4f4; padding: 15px; border-radius: 5px; margin: 15px 0; }
                    .footer { font-size: 0.8em; color: #777; text-align: center; margin-top: 20px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h2>Repair Ticket Status Update</h2>
                    </div>
                    <div class="content">
                        <p>Hello {{user.FirstName}},</p>
                        <p>The status of your repair ticket has been updated.</p>
                        <div class="status-box">
                            <p><strong>Ticket ID:</strong> #{{ticket.Id}}</p>
                            <p><strong>Title:</strong> {{ticket.Title}}</p>
                            <p><strong>Previous Status:</strong> {{previousStatus}}</p>
                            <p><strong>New Status:</strong> <span style="color: #28a745; font-weight: bold;">{{ticket.Status}}</span></p>
                        </div>
                        <p>Log in to LabTrack to view detailed notes or progress regarding this ticket.</p>
                    </div>
                    <div class="footer">
                        <p>This is an automated notification from the LabTrack System. Please do not reply directly to this email.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    public string GetPasswordResetTemplate(User user, string resetToken, string resetUrl)
    {
        var fullResetLink = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(user.Email)}";

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; }
                    .header { background-color: #dc3545; color: #ffffff; padding: 15px; text-align: center; border-radius: 8px 8px 0 0; }
                    .content { padding: 20px; text-align: center; }
                    .btn { display: inline-block; padding: 12px 24px; background-color: #dc3545; color: #ffffff; text-decoration: none; border-radius: 4px; font-weight: bold; margin: 20px 0; }
                    .footer { font-size: 0.8em; color: #777; text-align: center; margin-top: 20px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h2>Password Reset Request</h2>
                    </div>
                    <div class="content">
                        <p style="text-align: left;">Hello {{user.FirstName}},</p>
                        <p style="text-align: left;">We received a request to reset your password for your LabTrack account.</p>
                        <a href="{{fullResetLink}}" class="btn">Reset Password</a>
                        <p style="text-align: left; font-size: 0.9em; color: #666;">If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
                    </div>
                    <div class="footer">
                        <p>This is an automated notification from the LabTrack System. Please do not reply directly to this email.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}