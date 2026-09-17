using Google.GenAI;
using Google.GenAI.Types;
using System.Text.Json;

namespace CVBuilder.API.Services;

public class GeminiService
{
    private readonly Client _client;

    public GeminiService(IConfiguration configuration)
    {
        var apiKey = configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured.");
        }

        _client = new Client(apiKey: apiKey);
    }

    // =========================================================
    // COVER LETTER GENERATION
    // =========================================================

    public async Task<string> GenerateCoverLetterAsync(
        string candidateInformation,
        string jobTitle,
        string company,
        string jobDescription)
    {
        var prompt = $"""
        You are an expert professional career writer.

        Write a tailored cover letter for the candidate using ONLY the
        information provided below.

        CANDIDATE INFORMATION:
        {candidateInformation}

        JOB INFORMATION:
        Job Title: {jobTitle}
        Company: {company}

        JOB DESCRIPTION:
        {jobDescription}

        INSTRUCTIONS:

        1. Tailor the letter specifically to the job title, company,
           and job description.

        2. Identify the most relevant skills, experience, projects,
           and education from the candidate's CV.

        3. Connect the candidate's real experience to the requirements
           of the job.

        4. NEVER invent or assume:
           - skills
           - qualifications
           - certifications
           - work experience
           - companies
           - achievements
           - technologies
           - responsibilities
           - degrees
           - years of experience

        5. Do not claim the candidate has experience with something
           unless it appears in the candidate information.

        6. Do not copy the job description word-for-word.

        7. Make the letter sound natural and human rather than robotic.

        8. Keep it professional and concise.

        9. Aim for approximately 300-400 words.

        10. Do not include a subject line.

        11. Do not include placeholders such as:
            [Name]
            [Company]
            [Hiring Manager]

        12. Do not add fake contact information.

        13. Return ONLY the cover letter.
        """;

        var response = await _client.Models.GenerateContentAsync(
            model: "gemini-3.6-flash",
            contents: prompt
        );

        var text = response.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                "Gemini returned an empty response.");
        }

        return text.Trim();
    }

    // =========================================================
    // AI EXTERNAL JOB SEARCH
    // =========================================================

    // =========================================================
    // AI EXTERNAL JOB SEARCH
    // =========================================================

    public async Task<List<ExternalJobResult>> SearchJobsAsync(
        string query,
        string location)
    {
        var prompt = $$"""
    You are an AI job-search assistant for CareerFlow.

    The user is searching for jobs using:

    Job search: {{query}}
    Location: {{location}}

    IMPORTANT:
    You do NOT have access to live internet search in this request.

    Therefore:
    - Do NOT claim that you searched the internet.
    - Do NOT invent real companies.
    - Do NOT invent job vacancies.
    - Do NOT invent application URLs.
    - Do NOT fabricate salaries, dates, or job listings.

    For now, return an empty jobs array because CareerFlow will
    provide verified external vacancies through a separate job-data
    source.

    Return ONLY valid JSON using this exact structure:

    {
      "jobs": []
    }
    """;

        var response = await _client.Models.GenerateContentAsync(
            model: "gemini-3.6-flash",
            contents: prompt
        );

        var text = response.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                "Gemini returned an empty response."
            );
        }

        text = CleanJsonResponse(text);

        var result =
            JsonSerializer.Deserialize<ExternalJobSearchResponse>(
                text,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return result?.Jobs ?? new List<ExternalJobResult>();
    }

    private static string CleanJsonResponse(string text)
    {
        text = text.Trim();

        if (text.StartsWith("```json"))
        {
            text = text.Substring(7);
        }
        else if (text.StartsWith("```"))
        {
            text = text.Substring(3);
        }

        if (text.EndsWith("```"))
        {
            text = text.Substring(
                0,
                text.Length - 3);
        }

        return text.Trim();
    }
}


// =============================================================
// AI JOB SEARCH RESPONSE MODELS
// =============================================================

public class ExternalJobSearchResponse
{
    public List<ExternalJobResult> Jobs { get; set; } = new();
}


public class ExternalJobResult
{
    public string Title { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string WorkArrangement { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public string ExperienceLevel { get; set; } = string.Empty;

    public string Salary { get; set; } = string.Empty;

    public string PostedAt { get; set; } = string.Empty;

    public string ExternalUrl { get; set; } = string.Empty;

    public string SourceName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Skills { get; set; } = new();
}