using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;
using Thekdar.Services.Interface;
using Configuration = brevo_csharp.Client.Configuration;
using Task = System.Threading.Tasks.Task;

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
            <p>Hello <strong>{userName}</strong>,</p>
            <p>We received a request to reset your password for your Thekdar account.</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>";

        await SendEmailViaBrevoAsync(toEmail, userName, subject, body);
    }

    public async Task SendTwoFactorCodeAsync(string toEmail, string code, string userName)
    {
        var subject = "Your Two-Factor Authentication Code - Thekdar";
        var body = $@"
            <p>Hello <strong>{userName}</strong>,</p>
            <p>Use this code to complete your login: <strong>{code}</strong></p>
            <p>This code expires in 10 minutes.</p>";

        await SendEmailViaBrevoAsync(toEmail, userName, subject, body);
    }

    public async Task SendMobileCredentialsEmailAsync(string toEmail, string workerName, string password)
    {
        var subject = "Your Thekdar Mobile App Login Credentials";
        var body = $@"
            <p>Hello <strong>{workerName}</strong>,</p>
            <p>You have been given mobile access for Thekdar.</p>
            <p><strong>Login Credentials:</strong></p>
            <ul>
                <li><strong>Email:</strong> {toEmail}</li>
                <li><strong>Password:</strong> {password}</li>
            </ul>
            <p>Open the Thekdar app on your phone and login with these credentials.</p>
            <p>For security, you can change your password after logging in.</p>
            <p>For support, contact your contractor.</p>
            <p>Best regards,<br/>Thekdar Team</p>";

        await SendEmailViaBrevoAsync(toEmail, workerName, subject, body);
    }

    public async Task SendJobAssignmentEmailAsync(string toEmail, string workerName, string jobTitle, string clientName, string address, DateTime? scheduledDate, string assignedBy, string role, DateTime assignedAt)
    {
        var subject = $"New Job Assignment: {jobTitle} - Thekdar";
        var scheduledDateFormatted = scheduledDate.HasValue
            ? scheduledDate.Value.ToString("dddd, MMMM dd, yyyy")
            : "To be confirmed";
        var assignedAtFormatted = assignedAt.ToLocalTime().ToString("dddd, MMMM dd, yyyy h:mm tt");

        var body = $@"
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
            <p>Best regards,<br/>Thekdar Team</p>";

        await SendEmailViaBrevoAsync(toEmail, workerName, subject, body);
    }

    private async Task SendEmailViaBrevoAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var fromEmail = _configuration["Brevo:FromEmail"] ?? "noreplythaekdar@gmail.com";
            var fromName = _configuration["Brevo:FromName"] ?? "Thekdar";

            // Log configuration status
            _logger.LogInformation($"Brevo Config - HasApiKey: {!string.IsNullOrEmpty(apiKey)}, FromEmail: {fromEmail}, FromName: {fromName}");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Brevo API key is missing from configuration");
                throw new Exception("Email service not configured - API key missing");
            }

            // Configure Brevo API
            Configuration.Default.ApiKey.Clear();
            Configuration.Default.ApiKey.Add("api-key", apiKey);

            var apiInstance = new TransactionalEmailsApi();
            
            var sender = new SendSmtpEmailSender(fromName, fromEmail);
            var to = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(toEmail, toName) };
            
            var fullHtmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>{subject}</title>
                </head>
                <body style='font-family: Arial, sans-serif; padding: 20px;'>
                    {htmlBody}
                    <hr/>
                    <p style='color: #888; font-size: 12px;'>Thekdar - Contractor Management System</p>
                </body>
                </html>";
            
            var sendSmtpEmail = new SendSmtpEmail(
                sender: sender,
                to: to,
                subject: subject,
                htmlContent: fullHtmlContent
            );

            _logger.LogInformation($"Sending email via Brevo to {toEmail}");
            
            var response = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            
            _logger.LogInformation("Email sent successfully to {Email} via Brevo. MessageId: {MessageId}", 
                toEmail, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via Brevo. Error: {Message}", 
                toEmail, ex.Message);
            throw;
        }
    }
}