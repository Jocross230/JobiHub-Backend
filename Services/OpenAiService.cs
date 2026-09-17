using OpenAI.Responses;

namespace CVBuilder.API.Services;

#pragma warning disable OPENAI001

public class OpenAiService
{
    private readonly IConfiguration _configuration;

    public OpenAiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GenerateCoverLetterAsync(
        string candidateInformation,
        string jobDescription)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured.");
        }

        var client = new ResponsesClient(apiKey: apiKey);

        var prompt = $"""
        You are an expert professional career writer.

        Write a professional, personalized cover letter for the candidate
        based ONLY on the candidate information and job description provided.

        CANDIDATE INFORMATION:
        {candidateInformation}

        JOB DESCRIPTION:
        {jobDescription}

        REQUIREMENTS:
        - Do not invent experience, qualifications, certifications,
          achievements, companies, or technologies.
        - Match the candidate's genuine experience to the job requirements.
        - Make the letter specific to the job description.
        - Use a professional, confident and natural tone.
        - Avoid generic AI-sounding language.
        - Keep it approximately 300-450 words.
        - Do not create a fake company address.
        - Start with "Dear Hiring Manager,"
        - End professionally using the candidate's name.
        """;

        var response = await client.CreateResponseAsync(
            "gpt-5.1",
            prompt);

        return response.Value.GetOutputText();
    }
}

#pragma warning restore OPENAI001