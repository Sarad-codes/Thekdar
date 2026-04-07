using Thekdar.Services.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Thekdar.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string userName)
    {
        var subject = "Reset Your Password - Thekdar";
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Password Reset</title>
            </head>
            <body>
                <p>Hello <strong>{userName}</strong>,</p>
                <p>We received a request to reset your password for your Thekdar account.</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTwoFactorCodeAsync(string toEmail, string code, string userName)
    {
        var subject = "Your Two-Factor Authentication Code - Thekdar";
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>2FA Code</title>
            </head>
            <body>
                <p>Hello <strong>{userName}</strong>,</p>
                <p>Use this code to complete your login: <strong>{code}</strong></p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendJobAssignmentEmailAsync(string toEmail, string workerName, string jobTitle, string clientName, string address, DateTime? scheduledDate, string assignedBy, string role, DateTime assignedAt)
    {
        var subject = $"New Job Assignment: {jobTitle} - Thekdar";
        var scheduledDateFormatted = scheduledDate.HasValue
            ? scheduledDate.Value.ToString("dddd, MMMM dd, yyyy")
            : "To be confirmed";
        var assignedAtFormatted = assignedAt.ToLocalTime().ToString("dddd, MMMM dd, yyyy h:mm tt");

        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Job Assignment</title>
            </head>
            <body>
                <p>Hello <strong>{workerName}</strong>,</p>
                <p>You have been assigned to a new job.</p>
                <ul>
                    <li><strong>Job Title:</strong> {jobTitle}</li>
                    <li><strong>Client:</strong> {clientName}</li>
                    <li><strong>Location:</strong> {address}</li>
                    <li><strong>Scheduled Date:</strong> {scheduledDateFormatted}</li>
                    <li><strong>Role:</strong> {role}</li>
                    <li><strong>Assigned By:</strong> {assignedBy}</li>
                    <li><strong>Assigned At:</strong> {assignedAtFormatted}</li>
                </ul>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]!);
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]!);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
