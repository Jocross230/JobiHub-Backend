using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CVBuilder.API.Services;

public class JobAggregationService
{
    private readonly HttpClient _httpClient;

    public JobAggregationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ExternalJobResult>> SearchJobsAsync(
        string query,
        string location)
    {
        var searches = new[]
        {
            SearchRemotiveAsync(query, location),
            SearchJobicyAsync(query, location),
            SearchHimalayasAsync(query, location),
            SearchArbeitnowAsync(query, location),
            SearchRemoteOkAsync(query, location)
        };

        var results = await Task.WhenAll(searches);

        var allJobs = results
            .SelectMany(x => x)
            .ToList();

        var filtered = FilterJobs(
            allJobs,
            query,
            location);

        return filtered
            .Take(30)
            .ToList();
    }

    // =========================================================
    // REMOTIVE
    // =========================================================

    private async Task<List<ExternalJobResult>> SearchRemotiveAsync(
        string query,
        string location)
    {
        try
        {
            var url =
                $"https://remotive.com/api/remote-jobs?search={Uri.EscapeDataString(query)}";

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ExternalJobResult>();
            }

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<RemotiveResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (data?.Jobs == null)
            {
                return new List<ExternalJobResult>();
            }

            return data.Jobs
                .Select(job => new ExternalJobResult
                {
                    Title = job.Title ?? "",
                    Company = job.CompanyName ?? "",
                    Location = string.IsNullOrWhiteSpace(
                        job.CandidateRequiredLocation)
                        ? "Remote"
                        : job.CandidateRequiredLocation,
                    WorkArrangement = "Remote",
                    EmploymentType = job.JobType ?? "",
                    ExperienceLevel = job.Experience ?? "",
                    Salary = job.Salary ?? "",
                    PostedAt = job.PublicationDate ?? "",
                    ExternalUrl = job.Url ?? "",
                    SourceName = "Remotive",
                    Description = StripHtml(
                        job.Description ?? ""),
                    Skills = job.Tags ?? new List<string>()
                })
                .ToList();
        }
        catch
        {
            return new List<ExternalJobResult>();
        }
    }

    // =========================================================
    // JOBICY
    // =========================================================

    private async Task<List<ExternalJobResult>> SearchJobicyAsync(
        string query,
        string location)
    {
        try
        {
            var url =
                $"https://jobicy.com/api/v2/remote-jobs" +
                $"?count=50" +
                $"&tag={Uri.EscapeDataString(query)}";

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ExternalJobResult>();
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "jobs",
                    out var jobs))
            {
                return new List<ExternalJobResult>();
            }

            var results =
                new List<ExternalJobResult>();

            foreach (var job in jobs.EnumerateArray())
            {
                var title =
                    GetString(job, "jobTitle");

                var company =
                    GetString(job, "companyName");

                var description =
                    GetString(job, "jobDescription");

                var locationValue =
                    GetString(job, "jobGeo");

                var urlValue =
                    GetString(job, "url");

                var jobType =
                    GetArray(job, "jobType");

                var industry =
                    GetArray(job, "jobIndustry");

                var salaryMin =
                    GetDecimal(job, "salaryMin");

                var salaryMax =
                    GetDecimal(job, "salaryMax");

                var salaryCurrency =
                    GetString(job, "salaryCurrency");

                var salaryPeriod =
                    GetString(job, "salaryPeriod");

                var salary = "";

                if (salaryMin.HasValue ||
                    salaryMax.HasValue)
                {
                    salary =
                        $"{salaryCurrency} " +
                        $"{salaryMin?.ToString("N0") ?? ""}" +
                        $" - " +
                        $"{salaryMax?.ToString("N0") ?? ""}" +
                        $" {salaryPeriod}";
                }

                results.Add(new ExternalJobResult
                {
                    Title = title,
                    Company = company,
                    Location =
                        string.IsNullOrWhiteSpace(locationValue)
                            ? "Remote"
                            : locationValue,
                    WorkArrangement = "Remote",
                    EmploymentType =
                        string.Join(", ", jobType),
                    ExperienceLevel =
                        GetString(job, "jobLevel"),
                    Salary = salary,
                    PostedAt =
                        GetString(job, "pubDate"),
                    ExternalUrl = urlValue,
                    SourceName = "Jobicy",
                    Description =
                        StripHtml(description),
                    Skills = industry
                });
            }

            return results;
        }
        catch
        {
            return new List<ExternalJobResult>();
        }
    }

    // =========================================================
    // HIMALAYAS
    // =========================================================

    private async Task<List<ExternalJobResult>> SearchHimalayasAsync(
        string query,
        string location)
    {
        try
        {
            var url =
                $"https://himalayas.app/jobs/api/search" +
                $"?q={Uri.EscapeDataString(query)}" +
                $"&page=1";

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ExternalJobResult>();
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "jobs",
                    out var jobs))
            {
                return new List<ExternalJobResult>();
            }

            var results =
                new List<ExternalJobResult>();

            foreach (var job in jobs.EnumerateArray())
            {
                var title =
                    GetString(job, "title");

                var company =
                    GetString(job, "companyName");

                var description =
                    GetString(job, "description");

                var excerpt =
                    GetString(job, "excerpt");

                var employmentType =
                    GetString(job, "employmentType");

                var seniority =
                    GetString(job, "seniority");

                var applicationLink =
                    GetString(job, "applicationLink");

                var locationRestrictions =
                    GetLocationNames(
                        job,
                        "locationRestrictions");

                var categories =
                    GetArray(job, "categories");

                var minSalary =
                    GetDecimal(job, "minSalary");

                var maxSalary =
                    GetDecimal(job, "maxSalary");

                var currency =
                    GetString(job, "currency");

                var salaryPeriod =
                    GetString(job, "salaryPeriod");

                var salary = "";

                if (minSalary.HasValue ||
                    maxSalary.HasValue)
                {
                    salary =
                        $"{currency} " +
                        $"{minSalary?.ToString("N0") ?? ""}" +
                        $" - " +
                        $"{maxSalary?.ToString("N0") ?? ""}" +
                        $" {salaryPeriod}";
                }

                results.Add(new ExternalJobResult
                {
                    Title = title,
                    Company = company,
                    Location =
                        locationRestrictions.Count > 0
                            ? string.Join(
                                ", ",
                                locationRestrictions)
                            : "Worldwide / Remote",
                    WorkArrangement = "Remote",
                    EmploymentType =
                        employmentType,
                    ExperienceLevel =
                        seniority,
                    Salary = salary,
                    PostedAt =
                        GetString(job, "pubDate"),
                    ExternalUrl =
                        applicationLink,
                    SourceName = "Himalayas",
                    Description =
                        StripHtml(
                            string.IsNullOrWhiteSpace(
                                description)
                                ? excerpt
                                : description),
                    Skills = categories
                });
            }

            return results;
        }
        catch
        {
            return new List<ExternalJobResult>();
        }
    }

    // =========================================================
    // ARBEITNOW
    // =========================================================

    private async Task<List<ExternalJobResult>> SearchArbeitnowAsync(
        string query,
        string location)
    {
        try
        {
            var url =
                "https://www.arbeitnow.com/api/job-board-api";

            using var response =
                await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ExternalJobResult>();
            }

            var json =
                await response.Content.ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "data",
                    out var jobs))
            {
                return new List<ExternalJobResult>();
            }

            var results =
                new List<ExternalJobResult>();

            foreach (var job in jobs.EnumerateArray())
            {
                var title =
                    GetString(job, "title");

                var company =
                    GetString(job, "company_name");

                var description =
                    GetString(job, "description");

                var jobLocation =
                    GetString(job, "location");

                var urlValue =
                    GetString(job, "url");

                var remote =
                    GetBool(job, "remote");

                var tags =
                    GetArray(job, "tags");

                var searchableText =
                    $"{title} " +
                    $"{description} " +
                    $"{string.Join(" ", tags)}";

                var terms =
                    query
                        .Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                            x.ToLowerInvariant())
                        .ToList();

                var matches =
                    terms.Any(term =>
                        searchableText
                            .ToLowerInvariant()
                            .Contains(term));

                if (!matches)
                {
                    continue;
                }

                results.Add(new ExternalJobResult
                {
                    Title = title,
                    Company = company,
                    Location =
                        string.IsNullOrWhiteSpace(
                            jobLocation)
                            ? "Europe"
                            : jobLocation,
                    WorkArrangement =
                        remote
                            ? "Remote"
                            : "On-site / Hybrid",
                    EmploymentType =
                        GetArrayString(
                            job,
                            "job_types"),
                    ExperienceLevel = "",
                    Salary = "",
                    PostedAt =
                        GetString(job, "created_at"),
                    ExternalUrl = urlValue,
                    SourceName = "Arbeitnow",
                    Description =
                        StripHtml(description),
                    Skills = tags
                });
            }

            return results;
        }
        catch
        {
            return new List<ExternalJobResult>();
        }
    }

    // =========================================================
    // REMOTE OK
    // =========================================================

    private async Task<List<ExternalJobResult>> SearchRemoteOkAsync(
        string query,
        string location)
    {
        try
        {
            var url =
                $"https://remoteok.com/api";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            request.Headers.UserAgent.ParseAdd(
                "CareerFlow/1.0");

            using var response =
                await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ExternalJobResult>();
            }

            var json =
                await response.Content.ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(json);

            var results =
                new List<ExternalJobResult>();

            foreach (var job in document.RootElement
                         .EnumerateArray())
            {
                // Remote OK includes a metadata object
                // as the first array item.
                if (!job.TryGetProperty(
                        "position",
                        out _))
                {
                    continue;
                }

                var title =
                    GetString(job, "position");

                var company =
                    GetString(job, "company");

                var description =
                    GetString(job, "description");

                var tags =
                    GetArray(job, "tags");

                var jobUrl =
                    GetString(job, "url");

                var locationValue =
                    GetString(job, "location");

                var searchableText =
                    $"{title} " +
                    $"{description} " +
                    $"{string.Join(" ", tags)}";

                var terms =
                    query
                        .Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                            x.ToLowerInvariant())
                        .ToList();

                var matches =
                    terms.Any(term =>
                        searchableText
                            .ToLowerInvariant()
                            .Contains(term));

                if (!matches)
                {
                    continue;
                }

                results.Add(new ExternalJobResult
                {
                    Title = title,
                    Company = company,
                    Location =
                        string.IsNullOrWhiteSpace(
                            locationValue)
                            ? "Remote"
                            : locationValue,
                    WorkArrangement = "Remote",
                    EmploymentType =
                        GetString(job, "type"),
                    ExperienceLevel = "",
                    Salary =
                        BuildRemoteOkSalary(job),
                    PostedAt =
                        GetString(job, "date"),
                    ExternalUrl =
                        jobUrl,
                    SourceName = "Remote OK",
                    Description =
                        StripHtml(description),
                    Skills = tags
                });
            }

            return results;
        }
        catch
        {
            return new List<ExternalJobResult>();
        }
    }

    // =========================================================
    // FILTERING + RANKING
    // =========================================================

    private static List<ExternalJobResult> FilterJobs(
        List<ExternalJobResult> jobs,
        string query,
        string location)
    {
        var terms =
            query
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(x =>
                    x.ToLowerInvariant())
                .ToList();

        var requestedLocation =
            location.Trim().ToLowerInvariant();

        return jobs
            .Where(job =>
            {
                if (string.IsNullOrWhiteSpace(
                        job.ExternalUrl))
                {
                    return false;
                }

                var searchableText =
                    $"{job.Title} " +
                    $"{job.Company} " +
                    $"{job.Description} " +
                    $"{string.Join(
                        " ",
                        job.Skills)}";

                var lower =
                    searchableText.ToLowerInvariant();

                var matchingTerms =
                    terms.Count(term =>
                        lower.Contains(term));

                // At least one search term must match.
                return matchingTerms > 0;
            })
            .Where(job =>
            {
                if (string.IsNullOrWhiteSpace(
                        requestedLocation) ||
                    requestedLocation == "remote" ||
                    requestedLocation == "worldwide")
                {
                    return true;
                }

                var jobLocation =
                    job.Location?
                        .ToLowerInvariant() ?? "";

                var searchable =
                    $"{jobLocation} " +
                    $"{job.Description}"
                        .ToLowerInvariant();

                return searchable.Contains(
                           requestedLocation)
                       ||
                       searchable.Contains("worldwide")
                       ||
                       searchable.Contains("anywhere")
                       ||
                       searchable.Contains("remote");
            })
            .GroupBy(job =>
            {
                var normalizedUrl =
                    job.ExternalUrl
                        .Trim()
                        .TrimEnd('/')
                        .ToLowerInvariant();

                return normalizedUrl;
            })
            .Select(group =>
                group.First())
            .OrderByDescending(job =>
                CalculateRelevance(
                    job,
                    terms))
            .ThenByDescending(job =>
                ParseDate(job.PostedAt))
            .ToList();
    }

    private static int CalculateRelevance(
        ExternalJobResult job,
        List<string> terms)
    {
        var title =
            job.Title?.ToLowerInvariant() ?? "";

        var description =
            job.Description?.ToLowerInvariant() ?? "";

        var skills =
            string.Join(
                " ",
                job.Skills)
            .ToLowerInvariant();

        var score = 0;

        foreach (var term in terms)
        {
            if (title.Contains(term))
            {
                score += 10;
            }

            if (skills.Contains(term))
            {
                score += 5;
            }

            if (description.Contains(term))
            {
                score += 2;
            }
        }

        return score;
    }

    // =========================================================
    // JSON HELPERS
    // =========================================================

    private static string GetString(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String =>
                value.GetString() ?? "",

            JsonValueKind.Number =>
                value.ToString(),

            _ => ""
        };
    }

    private static bool GetBool(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
        {
            return false;
        }

        return value.ValueKind ==
                   JsonValueKind.True
               ||
               (
                   value.ValueKind ==
                       JsonValueKind.String
                   &&
                   bool.TryParse(
                       value.GetString(),
                       out var result)
                   &&
                   result
               );
    }

    private static decimal? GetDecimal(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
        {
            return null;
        }

        if (value.ValueKind ==
            JsonValueKind.Number)
        {
            if (value.TryGetDecimal(
                    out var number))
            {
                return number;
            }
        }

        if (value.ValueKind ==
            JsonValueKind.String)
        {
            if (decimal.TryParse(
                    value.GetString(),
                    out var number))
            {
                return number;
            }
        }

        return null;
    }

    private static List<string> GetArray(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
        {
            return new List<string>();
        }

        if (value.ValueKind !=
            JsonValueKind.Array)
        {
            return new List<string>();
        }

        return value
            .EnumerateArray()
            .Select(x =>
                x.ValueKind ==
                    JsonValueKind.String
                    ? x.GetString() ?? ""
                    : x.ToString())
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string GetArrayString(
        JsonElement element,
        string property)
    {
        return string.Join(
            ", ",
            GetArray(element, property));
    }

    private static List<string> GetLocationNames(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
        {
            return new List<string>();
        }

        if (value.ValueKind !=
            JsonValueKind.Array)
        {
            return new List<string>();
        }

        var locations =
            new List<string>();

        foreach (var item in
                 value.EnumerateArray())
        {
            if (item.ValueKind ==
                JsonValueKind.Object)
            {
                var name =
                    GetString(item, "name");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    locations.Add(name);
                }
            }
            else if (item.ValueKind ==
                     JsonValueKind.String)
            {
                locations.Add(
                    item.GetString() ?? "");
            }
        }

        return locations;
    }

    private static string BuildRemoteOkSalary(
        JsonElement job)
    {
        var min =
            GetDecimal(job, "salary_min");

        var max =
            GetDecimal(job, "salary_max");

        var currency =
            GetString(job, "salary_currency");

        if (!min.HasValue &&
            !max.HasValue)
        {
            return "";
        }

        return
            $"{currency} " +
            $"{min?.ToString("N0") ?? ""}" +
            $" - " +
            $"{max?.ToString("N0") ?? ""}";
    }

    private static DateTime ParseDate(
        string value)
    {
        if (DateTime.TryParse(
                value,
                out var date))
        {
            return date;
        }

        return DateTime.MinValue;
    }

    private static string StripHtml(
        string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        return Regex
            .Replace(
                html,
                "<.*?>",
                " ")
            .Replace(
                "&nbsp;",
                " ")
            .Trim();
    }

    // =========================================================
    // REMOTIVE DTOs
    // =========================================================

    private class RemotiveResponse
    {
        [JsonPropertyName("jobs")]
        public List<RemotiveJob> Jobs { get; set; } = new();
    }

    private class RemotiveJob
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

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
}