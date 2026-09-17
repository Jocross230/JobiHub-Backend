using CVBuilder.API.Data;
using CVBuilder.API.Models;
using CVBuilder.API.Models.Auth;
using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CvBuilderDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ActivityLogService _activityLogService;
    private readonly ResendEmailService _resendEmailService;

    public AuthController(
        CvBuilderDbContext context,
        IConfiguration configuration,
        ActivityLogService activityLogService,
        ResendEmailService resendEmailService)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
        _activityLogService = activityLogService;
        _resendEmailService = resendEmailService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new
            {
                message = "Full name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Password is required."
            });
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        if (existingUser != null)
        {
            return BadRequest(new
            {
                message = "An account with this email already exists."
            });
        }

        var role = request.Role?.Trim();

        if (string.IsNullOrWhiteSpace(role))
        {
            role = "JobSeeker";
        }

        if (!role.Equals("JobSeeker", StringComparison.OrdinalIgnoreCase) &&
            !role.Equals("Business", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Invalid account type."
            });
        }

        role = role.Equals("Business", StringComparison.OrdinalIgnoreCase)
            ? "Business"
            : "JobSeeker";

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            action: "User registered",
            actorId: user.Id,
            actorName: user.FullName,
            targetType: "User",
            targetId: user.Id.ToString(),
            details: $"New {user.Role} account registered."
        );

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new
            {
                message = "This account is no longer active."
            });
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        if (string.IsNullOrWhiteSpace(user.Role))
        {
            user.Role = "JobSeeker";
            await _context.SaveChangesAsync();
        }

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    // POST: api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return BadRequest(new
            {
                message = "Current password is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new
            {
                message = "New password is required."
            });
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return BadRequest(new
            {
                message = "New passwords do not match."
            });
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new
            {
                message = "New password must be at least 6 characters."
            });
        }

        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user authentication."
            });
        }

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword
        );

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return BadRequest(new
            {
                message = "Current password is incorrect."
            });
        }

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.NewPassword
        );

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            action: "Password changed",
            actorId: user.Id,
            actorName: user.FullName,
            targetType: "User",
            targetId: user.Id.ToString(),
            details: "User changed their password."
        );

        return Ok(new
        {
            message = "Password changed successfully."
        });
    }

    // POST: api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        var email = request.Email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        // Always return the same response so we don't reveal
        // whether an email belongs to an account.
        if (user == null || !user.IsActive)
        {
            return Ok(new
            {
                message = "If an account exists for this email, a password reset link has been sent."
            });
        }

        // Invalidate previous unused reset tokens.
        var oldTokens = await _context.PasswordResetTokens
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var oldToken in oldTokens)
        {
            oldToken.IsUsed = true;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);

        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);

        await _context.SaveChangesAsync();

        var frontendUrl =
            _configuration["Frontend:BaseUrl"]
            ?? "http://localhost:8443";

        var resetLink =
            $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

        await _resendEmailService.SendPasswordResetEmailAsync(
            user.Email,
            resetLink
        );

        return Ok(new
        {
            message = "If an account exists for this email, a password reset link has been sent."
        });
    }

    // POST: api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new
            {
                message = "Reset token is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new
            {
                message = "New password is required."
            });
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return BadRequest(new
            {
                message = "New passwords do not match."
            });
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new
            {
                message = "New password must be at least 6 characters."
            });
        }

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(x =>
                x.Token == request.Token &&
                !x.IsUsed);

        if (resetToken == null)
        {
            return BadRequest(new
            {
                message = "This password reset link is invalid."
            });
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            resetToken.IsUsed = true;
            await _context.SaveChangesAsync();

            return BadRequest(new
            {
                message = "This password reset link has expired."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == resetToken.UserId);

        if (user == null || !user.IsActive)
        {
            return BadRequest(new
            {
                message = "Unable to reset this password."
            });
        }

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.NewPassword
        );

        // Mark this token as used.
        resetToken.IsUsed = true;

        // Invalidate any other reset tokens for this user.
        var otherTokens = await _context.PasswordResetTokens
            .Where(x =>
                x.UserId == user.Id &&
                x.Id != resetToken.Id &&
                !x.IsUsed)
            .ToListAsync();

        foreach (var token in otherTokens)
        {
            token.IsUsed = true;
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            action: "Password reset",
            actorId: user.Id,
            actorName: user.FullName,
            targetType: "User",
            targetId: user.Id.ToString(),
            details: "User reset their password using a password reset link."
        );

        return Ok(new
        {
            message = "Password reset successfully."
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT key is not configured."
            );
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                user.FullName
            ),

            new Claim(
                ClaimTypes.Email,
                user.Email
            ),

            new Claim(
                ClaimTypes.Role,
                user.Role
            )
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
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}