using System.Security.Claims;
using CVBuilder.API.Data;
using CVBuilder.API.DTOs;
using CVBuilder.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/business")]
[Authorize(Roles = "Business")]
public class BusinessController : ControllerBase
{
    private readonly CvBuilderDbContext _context;


    public BusinessController(CvBuilderDbContext context)
    {
        _context = context;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (business == null)
        {
            return NotFound(new
            {
                message = "Business profile not found."
            });
        }

        return Ok(business);
    }

    [HttpPost("profile")]
    public async Task<IActionResult> SaveProfile([FromBody] BusinessRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (business == null)
        {
            business = new Business
            {
                UserId = userId,
                CompanyName = request.CompanyName,
                Industry = request.Industry,
                Description = request.Description,
                Website = request.Website,
                Location = request.Location,
                CompanySize = request.CompanySize,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Businesses.Add(business);
        }
        else
        {
            business.CompanyName = request.CompanyName;
            business.Industry = request.Industry;
            business.Description = request.Description;
            business.Website = request.Website;
            business.Location = request.Location;
            business.CompanySize = request.CompanySize;
            business.ContactEmail = request.ContactEmail;
            business.ContactPhone = request.ContactPhone;
            business.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Business profile saved successfully.",
            business
        });
    }
}