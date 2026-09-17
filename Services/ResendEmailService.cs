using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CVBuilder.API.Services;

public class ResendEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ResendEmailService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetLink)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var fromEmail = _configuration["Resend:FromEmail"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Resend API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "Resend FromEmail is not configured.");
        }

        var payload = new
        {
            from = fromEmail,
            to = new[] { recipientEmail },
            subject = "Reset your CareerFlow password",
            html = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: auto;">
                    <h2>Reset your CareerFlow password</h2>

                    <p>We received a request to reset your CareerFlow password.</p>

                    <p>
                        Click the button below to choose a new password:
                    </p>

                    <p>
                        <a href="{resetLink}"
                           style="
                               display: inline-block;
                               padding: 12px 20px;
                               background-color: #2563eb;
                               color: white;
                               text-decoration: none;
                               border-radius: 6px;
                           ">
                            Reset Password
                        </a>
                    </p>

                    <p>
                        This link will expire in 30 minutes.
                    </p>

                    <p>
                        If you did not request a password reset, you can safely
                        ignore this email.
                    </p>

                    <p>
                        — CareerFlow
                    </p>
                </div>
                """
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.resend.com/emails");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Resend email failed. Status: {(int)response.StatusCode}. Details: {error}");
        }
    }
}