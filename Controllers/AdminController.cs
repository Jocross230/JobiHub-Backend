using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CVBuilder.API.Data;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly CvBuilderDbContext _context;

    public AdminController(
        IConfiguration configuration,
        CvBuilderDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var username = _configuration["Admin:Username"];
        var password = _configuration["Admin:Password"];
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(jwtKey))
        {
            return StatusCode(500, "Admin authentication is not configured.");
        }

        if (request.Username != username ||
            request.Password != password)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return Ok(new
        {
            token = tokenString
        });
    }

    // ==========================================
    // ADMIN DASHBOARD STATISTICS
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var totalUsers = await _context.Users.CountAsync();

        var totalCvs = await _context.Cvs.CountAsync();

        var cvsToday = await _context.Cvs.CountAsync(x =>
            x.CreatedAt >= today &&
            x.CreatedAt < tomorrow);

        var totalBusinesses = await _context.Businesses.CountAsync();

        // Business does not currently have a Status property,
        // so all existing businesses are counted as active for now.
        var activeBusinesses = totalBusinesses;

        var totalJobs = await _context.Jobs.CountAsync();

        var activeJobs = await _context.Jobs.CountAsync(x =>
            x.Status.ToLower() == "published");

        var totalApplications = await _context.Applications.CountAsync();

        var openRecruitmentRequests =
            await _context.RecruitmentRequests.CountAsync(x =>
                x.Status == "Pending" ||
                x.Status == "Under Review" ||
                x.Status == "In Progress" ||
                x.Status == "Candidates Sourced" ||
                x.Status == "Interview Stage");

        // Count active support issues
        var openSupportIssues =
            await _context.SupportIssues.CountAsync(x =>
                x.Status == "Open" ||
                x.Status == "In Progress" ||
                x.Status == "Waiting for User");

        return Ok(new
        {
            totalUsers = totalUsers,
            activeUsers = totalUsers,

            totalCvs = totalCvs,
            cvsToday = cvsToday,

            totalBusinesses = totalBusinesses,
            activeBusinesses = activeBusinesses,

            totalJobs = totalJobs,
            activeJobs = activeJobs,

            totalApplications = totalApplications,

            openRecruitmentRequests = openRecruitmentRequests,

            openSupportIssues = openSupportIssues
        });
    }
}

public class AdminLoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}