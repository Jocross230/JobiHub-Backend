using System.Security.Claims;
using CVBuilder.API.Data;
using CVBuilder.API.DTOs;
using CVBuilder.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public PaymentsController(CvBuilderDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // SUBMIT PAYMENT
    // ============================================================

    [HttpPost("request")]
    public async Task<IActionResult> SubmitPaymentRequest(
        [FromBody] PaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Product))
            return BadRequest(new
            {
                message = "Product is required."
            });

        if (string.IsNullOrWhiteSpace(request.PaymentReference))
            return BadRequest(new
            {
                message = "Payment reference is required."
            });

        if (request.Amount <= 0)
            return BadRequest(new
            {
                message = "Invalid payment amount."
            });

        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new
            {
                message = "User could not be identified."
            });

        var allowedProducts = new Dictionary<string, decimal>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["PremiumCV"] = 2000m,
            ["CoverLetterPack"] = 1000m,
            ["JobReady"] = 2500m
        };

        if (!allowedProducts.TryGetValue(
                request.Product,
                out var expectedAmount))
        {
            return BadRequest(new
            {
                message = "Invalid product."
            });
        }

        if (request.Amount != expectedAmount)
        {
            return BadRequest(new
            {
                message =
                    "The payment amount does not match the selected product."
            });
        }

        var payment = new Payment
        {
            UserId = userId,
            Product = request.Product.Trim(),
            Amount = request.Amount,
            PaymentReference = request.PaymentReference.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null
        };

        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment submitted for verification.",
            paymentId = payment.Id,
            status = payment.Status
        });
    }


    // ============================================================
    // MY PAYMENTS
    // ============================================================

    [HttpGet("my-payments")]
    public async Task<IActionResult> GetMyPayments()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var payments = await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Product,
                p.Amount,
                p.PaymentReference,
                p.Status,
                p.CreatedAt,
                p.ReviewedAt,
                p.ExpiresAt
            })
            .ToListAsync();

        return Ok(payments);
    }


    // ============================================================
    // ADMIN - ALL PAYMENTS
    // ============================================================

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPayments()
    {
        var payments = await _context.Payments
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.UserId,

                UserName =
                    p.User != null
                        ? p.User.FullName
                        : "",

                UserEmail =
                    p.User != null
                        ? p.User.Email
                        : "",

                p.Product,
                p.Amount,
                p.PaymentReference,
                p.Status,
                p.CreatedAt,
                p.ReviewedAt,
                p.ExpiresAt
            })
            .ToListAsync();

        return Ok(payments);
    }


    // ============================================================
    // ADMIN - APPROVE / REJECT PAYMENT
    // ============================================================

    [HttpPut("admin/{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewPayment(
        int id,
        [FromBody] PaymentReviewRequest request)
    {
        if (request.Status != "Approved" &&
            request.Status != "Rejected")
        {
            return BadRequest(new
            {
                message = "Status must be Approved or Rejected."
            });
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound(new
            {
                message = "Payment not found."
            });
        }

        payment.Status = request.Status;
        payment.ReviewedAt = DateTime.UtcNow;

        // ========================================================
        // SET PAYMENT EXPIRATION
        // ========================================================

        if (request.Status == "Approved")
        {
            payment.ExpiresAt = payment.Product switch
            {
                "PremiumCV" =>
                    payment.ReviewedAt.Value.AddMonths(1),

                "CoverLetterPack" =>
                    payment.ReviewedAt.Value.AddDays(7),

                "JobReady" =>
                    payment.ReviewedAt.Value.AddMonths(1),

                _ => null
            };
        }
        else
        {
            payment.ExpiresAt = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                $"Payment {request.Status.ToLower()} successfully.",

            paymentId = payment.Id,

            status = payment.Status,

            expiresAt = payment.ExpiresAt
        });
    }


    // ============================================================
    // CHECK PAYMENT STATUS
    // ============================================================

    [HttpGet("status")]
    public async Task<IActionResult> GetPaymentStatus(
        [FromQuery] string product)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return BadRequest(new
            {
                message = "Product is required."
            });
        }

        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;

        var payment = await _context.Payments
            .Where(p =>
                p.UserId == userId &&
                p.Product == product &&
                p.Status == "Approved" &&
                p.ExpiresAt != null &&
                p.ExpiresAt > now)
            .OrderByDescending(p => p.ExpiresAt)
            .FirstOrDefaultAsync();

        if (payment == null)
        {
            return Ok(new
            {
                approved = false,
                product,
                status = "NotApproved",
                expiresAt = (DateTime?)null
            });
        }

        return Ok(new
        {
            approved = true,
            product,
            status = "Approved",
            expiresAt = payment.ExpiresAt
        });
    }
}