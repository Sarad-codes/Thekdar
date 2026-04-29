using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thekdar.Data;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Thekdar.Controllers.ApiControllers;

[ApiController]
[Route("api/mobile/[controller]")]
public class MobileAuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<MobileAuthController> _logger;

    public MobileAuthController(
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmployeeService employeeService,
        ILogger<MobileAuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            // Find worker by email
            var employee = await _employeeService.GetByEmailForMobileAsync(request.Email);
            
            if (employee == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Verify password
            var isValid = await _employeeService.VerifyMobilePasswordAsync(request.Email, request.Password);
            
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Update last login time
            await _employeeService.UpdateLastMobileLoginAsync(employee.Id);

            // Get contractor info
            var contractor = await _context.Users.FindAsync(employee.ContractorId);

            // Generate JWT token
            var token = GenerateJwtToken(employee);

            return Ok(new MobileLoginResponse
            {
                Token = token,
                UserId = employee.Id,
                Name = employee.FullName,
                Email = employee.Email,
                Phone = employee.Phone1,
                ContractorId = employee.ContractorId,
                ContractorName = contractor?.Name,
                Role = "Worker"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during mobile login for email {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    private string GenerateJwtToken(EmployeeModel employee)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "your-super-secret-key-here-at-least-32-characters-long"));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Name, employee.FullName),
            new Claim(ClaimTypes.Email, employee.Email ?? ""),
            new Claim(ClaimTypes.Role, "Worker"),
            new Claim("ContractorId", employee.ContractorId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ThekdarAPI",
            audience: _configuration["Jwt:Audience"] ?? "ThekdarMobileApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Request/Response Models
public class MobileLoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class MobileLoginResponse
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int ContractorId { get; set; }
    public string ContractorName { get; set; }
    public string Role { get; set; }
}