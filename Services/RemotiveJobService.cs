using System.Text.Json;
using System.Text.Json.Serialization;

namespace CVBuilder.API.Services;

public class RemotiveJobService
{
    private readonly HttpClient _httpClient;

    public RemotiveJobService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ExternalJobResult>> SearchJobsAsync(
        string query,
        string location)
    {
        var url =
            $"https://remotive.com/api/remote-jobs?search={Uri.EscapeDataString(query)}";

        using var response = await _httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var data = JsonSerializer.Deserialize<RemotiveResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (data?.Jobs == null || data.Jobs.Count == 0)
        {
            return new List<ExternalJobResult>();
        }

        var searchTerms = query
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToList();

        var filteredJobs = data.Jobs
            .Where(job =>
            {
                var title = job.Title?.ToLowerInvariant() ?? "";
                var description = job.Description?.ToLowerInvariant() ?? "";
                var category = job.Category?.ToLowerInvariant() ?? "";
                var tags = job.Tags != null
                    ? string.Join(" ", job.Tags).ToLowerInvariant()
                    : "";

                var searchableText =
                    $"{title} {description} {category} {tags}";

                return searchTerms.Any(term =>
                    searchableText.Contains(term));
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(location))
        {
            var requestedLocation = location.Trim().ToLowerInvariant();

            filteredJobs = filteredJobs
                .Where(job =>
                {
                    var jobLocation =
                        job.CandidateRequiredLocation?
                            .ToLowerInvariant() ?? "";

                    if (requestedLocation == "remote")
                    {
                        return true;
                    }

                    return jobLocation.Contains(requestedLocation) ||
                           jobLocation.Contains("worldwide") ||
                           jobLocation.Contains("anywhere");
                })
                .ToList();
        }

        return filteredJobs
            .Take(10)
            .Select(job => new ExternalJobResult
            {
                Title = job.Title ?? string.Empty,

                Company = job.CompanyName ?? string.Empty,

                Location =
                    string.IsNullOrWhiteSpace(
                        job.CandidateRequiredLocation)
                        ? "Remote"
                        : job.CandidateRequiredLocation,

                WorkArrangement = "Remote",

                EmploymentType =
                    job.JobType ?? string.Empty,

                ExperienceLevel =
                    job.Experience ?? string.Empty,

                Salary =
                    job.Salary ?? string.Empty,

                PostedAt =
                    job.PublicationDate ?? string.Empty,

                ExternalUrl =
                    job.Url ?? string.Empty,

                SourceName = "Remotive",

                Description =
                    StripHtml(
                        job.Description ?? string.Empty),

                Skills =
                    job.Tags ?? new List<string>()
            })
            .ToList();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex
            .Replace(html, "<.*?>", " ")
            .Trim();
    }
}

public class RemotiveResponse
{
    [JsonPropertyName("jobs")]
    public List<RemotiveJob> Jobs { get; set; } = new();
}

public class RemotiveJob
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("company_logo")]
    public string? CompanyLogo { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("job_type")]
    public string? JobType { get; set; }

    [JsonPropertyName("publication_date")]
    public string? PublicationDate { get; set; }

    [JsonPropertyName("candidate_required_location")]
    public string? CandidateRequiredLocation { get; set; }

    [JsonPropertyName("salary")]
    public string? Salary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("experience")]
    public string? Experience { get; set; }
}