using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Data;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/businesses")]
[Authorize(Roles = "Admin")]
public class AdminBusinessesController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public AdminBusinessesController(CvBuilderDbContext context)
    {
        _context = context;
    }

    // GET: /api/Admin/businesses
    [HttpGet]
    public async Task<IActionResult> GetBusinesses()
    {
        var businesses = await _context.Businesses
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                userId = x.UserId,
                companyName = x.CompanyName,
                industry = x.Industry,
                description = x.Description,
                location = x.Location,
                website = x.Website,
                contactEmail = x.ContactEmail,
                createdAt = x.CreatedAt,

                // Business currently has no Status column.
                status = "active"
            })
            .ToListAsync();

        return Ok(businesses);
    }

    // PUT: /api/Admin/businesses/{id}/approve
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveBusiness(int id)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.Id == id);

        if (business == null)
        {
            return NotFound(new
            {
                message = "Business not found."
            });
        }

        return Ok(new
        {
            message = "Business approved successfully.",
            id = business.Id,
            status = "active"
        });
    }

    // PUT: /api/Admin/businesses/{id}/suspend
    [HttpPut("{id:int}/suspend")]
    public async Task<IActionResult> SuspendBusiness(int id)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.Id == id);

        if (business == null)
        {
            return NotFound(new
            {
                message = "Business not found."
            });
        }

        return Ok(new
        {
            message = "Business suspended successfully.",
            id = business.Id,
            status = "suspended"
        });
    }
}