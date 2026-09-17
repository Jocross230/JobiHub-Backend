using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CVBuilder.API.Data;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/Admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly CvBuilderDbContext _context;

    public AdminAnalyticsController(CvBuilderDbContext context)
    {
        _context = context;
    }

    // GET: /api/Admin/analytics?period=Monthly
    [HttpGet]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] string period = "Monthly")
    {
        period = period.Trim().ToLower();

        DateTime startDate;
        DateTime endDate = DateTime.UtcNow;

        switch (period)
        {
            case "daily":
                startDate = DateTime.UtcNow.Date.AddDays(-30);
                break;

            case "weekly":
                startDate = DateTime.UtcNow.Date.AddDays(-84);
                break;

            case "yearly":
                startDate = DateTime.UtcNow.Date.AddYears(-5);
                break;

            case "monthly":
            default:
                startDate = DateTime.UtcNow.Date.AddMonths(-12);
                break;
        }

        var users = await _context.Users
            .AsNoTracking()
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .Select(x => x.CreatedAt)
            .ToListAsync();

        var cvs = await _context.Cvs
            .AsNoTracking()
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .Select(x => x.CreatedAt)
            .ToListAsync();

        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .Select(x => x.CreatedAt)
            .ToListAsync();

        var businesses = await _context.Businesses
            .AsNoTracking()
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .Select(x => x.CreatedAt)
            .ToListAsync();

        var userGrowth = BuildSeries(users, startDate, period);
        var cvCreation = BuildSeries(cvs, startDate, period);
        var jobPostings = BuildSeries(jobs, startDate, period);
        var businessRegistrations = BuildSeries(businesses, startDate, period);

        return Ok(new
        {
            userGrowth,
            cvCreation,
            jobPostings,
            businessRegistrations
        });
    }

    private static List<object> BuildSeries(
        List<DateTime> dates,
        DateTime startDate,
        string period)
    {
        var result = new List<object>();

        if (period == "daily")
        {
            for (var date = startDate.Date;
                 date <= DateTime.UtcNow.Date;
                 date = date.AddDays(1))
            {
                var nextDate = date.AddDays(1);

                result.Add(new
                {
                    date = date.ToString("MMM dd"),
                    count = dates.Count(x =>
                        x >= date &&
                        x < nextDate)
                });
            }
        }
        else if (period == "weekly")
        {
            for (var date = startDate.Date;
                 date <= DateTime.UtcNow.Date;
                 date = date.AddDays(7))
            {
                var nextDate = date.AddDays(7);

                result.Add(new
                {
                    date = date.ToString("MMM dd"),
                    count = dates.Count(x =>
                        x >= date &&
                        x < nextDate)
                });
            }
        }
        else if (period == "yearly")
        {
            for (var year = startDate.Year;
                 year <= DateTime.UtcNow.Year;
                 year++)
            {
                var nextYear = year + 1;

                result.Add(new
                {
                    date = year.ToString(),
                    count = dates.Count(x =>
                        x.Year == year)
                });
            }
        }
        else
        {
            var firstMonth = new DateTime(
                startDate.Year,
                startDate.Month,
                1);

            var currentMonth = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                1);

            for (var month = firstMonth;
                 month <= currentMonth;
                 month = month.AddMonths(1))
            {
                var nextMonth = month.AddMonths(1);

                result.Add(new
                {
                    date = month.ToString("MMM"),
                    count = dates.Count(x =>
                        x >= month &&
                        x < nextMonth)
                });
            }
        }

        return result;
    }
}