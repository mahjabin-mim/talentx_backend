using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Dtos.Jobs;
using Shared.Exceptions;

namespace Infrastructure.Services;

public sealed class GeminiJobDescriptionGenerationService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiJobDescriptionGenerationService> logger) : IJobDescriptionGenerationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly ILogger<GeminiJobDescriptionGenerationService> _logger = logger;

    public async Task<string> GenerateAsync(
        GenerateJobDescriptionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            text = BuildPrompt(request)
                        }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseJsonSchema = BuildResponseSchema()
            }
        };

        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/v1beta/models/{_options.Model}:generateContent";
        var maxRetryAttempts = Math.Max(1, _options.MaxRetryAttempts);
        var initialRetryDelayMilliseconds = Math.Max(250, _options.InitialRetryDelayMilliseconds);

        _logger.LogInformation(
            "Generating job description with Gemini. Model: {Model}, JobId: {JobId}, MaxRetryAttempts: {MaxRetryAttempts}",
            _options.Model,
            request.JobId,
            maxRetryAttempts);

        for (var attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ParseGeneratedDescription(responseBody);
                }

                if (IsTransientStatusCode(response.StatusCode))
                {
                    if (attempt == maxRetryAttempts)
                    {
                        _logger.LogInformation(
                            "Gemini job description generation remained unavailable after {AttemptCount} attempts. Status: {StatusCode}, Body: {ResponseBody}",
                            attempt,
                            (int)response.StatusCode,
                            Truncate(responseBody));

                        throw new JobDescriptionGenerationUnavailableException(
                            "Job description generation is temporarily unavailable. Please try again shortly.");
                    }

                    var retryDelay = GetRetryDelay(attempt, initialRetryDelayMilliseconds);

                    _logger.LogInformation(
                        "Transient Gemini job description generation failure on attempt {Attempt}/{MaxRetryAttempts}. Status: {StatusCode}. Retrying in {RetryDelayMs}ms.",
                        attempt,
                        maxRetryAttempts,
                        (int)response.StatusCode,
                        retryDelay.TotalMilliseconds);

                    await Task.Delay(retryDelay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Gemini job description generation failed with status {(int)response.StatusCode}: {responseBody}");
            }
            catch (HttpRequestException ex) when (attempt < maxRetryAttempts)
            {
                var retryDelay = GetRetryDelay(attempt, initialRetryDelayMilliseconds);

                _logger.LogInformation(
                    ex,
                    "Network error during Gemini job description generation on attempt {Attempt}/{MaxRetryAttempts}. Retrying in {RetryDelayMs}ms.",
                    attempt,
                    maxRetryAttempts,
                    retryDelay.TotalMilliseconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation(ex, "Gemini job description generation failed because the upstream service could not be reached.");

                throw new JobDescriptionGenerationUnavailableException(
                    "Job description generation is temporarily unavailable. Please try again shortly.");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < maxRetryAttempts)
            {
                var retryDelay = GetRetryDelay(attempt, initialRetryDelayMilliseconds);

                _logger.LogInformation(
                    ex,
                    "Gemini job description generation timed out on attempt {Attempt}/{MaxRetryAttempts}. Retrying in {RetryDelayMs}ms.",
                    attempt,
                    maxRetryAttempts,
                    retryDelay.TotalMilliseconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(ex, "Gemini job description generation timed out.");

                throw new JobDescriptionGenerationUnavailableException(
                    "Job description generation is temporarily unavailable. Please try again shortly.");
            }
        }

        throw new JobDescriptionGenerationUnavailableException(
            "Job description generation is temporarily unavailable. Please try again shortly.");
    }

    private static string BuildPrompt(GenerateJobDescriptionRequestDto request)
    {
        var location = request.WorkArrangement.Equals("Remote", StringComparison.OrdinalIgnoreCase)
            ? $"Remote, timezone: {request.Timezone}"
            : $"{request.City}, {request.CountryCode}";

        return
            "Generate a polished, concise job description for a hiring platform.\n" +
            "Return only JSON that matches the provided schema exactly.\n" +
            "Rules:\n" +
            "- Use a professional but direct tone.\n" +
            "- Do not invent benefits, company facts, application steps, or legal language.\n" +
            "- Use the provided title, responsibilities, skills, seniority, work arrangement, compensation, and deadline.\n" +
            "- Write only the description body. Do not include markdown fences or extra JSON properties.\n\n" +
            $"Title: {request.Title}\n" +
            $"Existing description/context: {request.Description}\n" +
            $"Responsibilities: {request.Responsibilities}\n" +
            $"Skills: {string.Join(", ", request.Skills)}\n" +
            $"Employment type: {request.EmploymentType}\n" +
            $"Work arrangement: {request.WorkArrangement}\n" +
            $"Location: {location}\n" +
            $"Experience level: {request.ExperienceLevel}\n" +
            $"Experience years: {request.ExperienceMinYears}-{request.ExperienceMaxYears}\n" +
            $"Salary range: {request.SalaryMin}-{request.SalaryMax} {request.CurrencyCode}\n" +
            $"Open positions: {request.NumberOfPositions}\n" +
            $"Application deadline: {request.ApplicationDeadline:O}\n";
    }

    private static object BuildResponseSchema() =>
        new
        {
            type = "object",
            properties = new
            {
                description = new
                {
                    type = "string",
                    description = "AI generated job description."
                }
            },
            required = new[] { "description" },
            additionalProperties = false
        };

    private static string ParseGeneratedDescription(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no candidates.");
        }

        var jsonText = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("Gemini returned an empty job description response.");
        }

        var result = JsonSerializer.Deserialize<GeneratedJobDescriptionResult>(jsonText, SerializerOptions);
        var description = result?.Description?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Gemini returned an empty generated job description.");
        }

        return description.Length <= 4000 ? description : description[..4000];
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(int attempt, int initialDelayMilliseconds)
    {
        var multiplier = Math.Pow(2, attempt - 1);
        var delayMilliseconds = Math.Min(initialDelayMilliseconds * multiplier, 5000);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private static string Truncate(string value, int maxLength = 500)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}...";
    }

    private sealed class GeneratedJobDescriptionResult
    {
        public string? Description { get; init; }
    }
}
