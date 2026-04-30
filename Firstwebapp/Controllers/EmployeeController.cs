using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Thekdar.Models;
using Thekdar.Services.Interface;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Thekdar.Controllers;

[Authorize(Roles = "Admin,Contractor")]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeController> _logger;
    private readonly IConfiguration _configuration;  // ← ADD THIS

    // UPDATE CONSTRUCTOR
    public EmployeeController(IEmployeeService employeeService, IEmailService emailService, ILogger<EmployeeController> logger, IConfiguration configuration)
    {
        _employeeService = employeeService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;  // ← ADD THIS
    }

    // ... rest of your code stays the same ...

    // ========== TEST ENDPOINTS FOR BREVO DEBUGGING ==========

    [AllowAnonymous]
    [HttpGet("test-brevo-config")]
    public IActionResult TestBrevoConfig()
    {
        try
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var fromEmail = _configuration["Brevo:FromEmail"];
            var fromName = _configuration["Brevo:FromName"];
        
            return Ok(new { 
                hasApiKey = !string.IsNullOrEmpty(apiKey),
                apiKeyLength = apiKey?.Length ?? 0,
                apiKeyPrefix = apiKey?.Substring(0, Math.Min(15, apiKey?.Length ?? 0)) + "...",
                fromEmail = fromEmail,
                fromName = fromName,
                environment = _configuration["ASPNETCORE_ENVIRONMENT"]
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("send-test-email")]
    public async Task<IActionResult> SendTestEmail(string toEmail = "noreplythaekdar@gmail.com")
    {
        try
        {
            _logger.LogInformation($"Attempting to send test email to {toEmail}");
        
            await _emailService.SendMobileCredentialsEmailAsync(
                toEmail, 
                "Test Worker", 
                "TestPassword123"
            );
        
            _logger.LogInformation($"Test email sent successfully to {toEmail}");
            return Ok(new { success = true, message = $"Email sent to {toEmail}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email");
            return StatusCode(500, new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}