namespace Thekdar.Services.Interface;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string userName);
    Task SendTwoFactorCodeAsync(string toEmail, string code, string userName);
    Task SendMobileCredentialsEmailAsync(string toEmail, string workerName, string password);
    Task SendJobAssignmentEmailAsync(string toEmail, string workerName, string jobTitle, string clientName, string address, DateTime? scheduledDate, string assignedBy, string role, DateTime assignedAt);
}