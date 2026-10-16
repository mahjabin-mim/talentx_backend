using System.Net.Http.Json;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Dtos.Applications;
using Shared.Exceptions;

namespace Infrastructure.Services;

public sealed class GeminiResumeExtractionService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiResumeExtractionService> logger) : IResumeExtractionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly ILogger<GeminiResumeExtractionService> _logger = logger;

    public async Task<ExtractApplicationResumeDto> ExtractAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        await using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        var base64File = Convert.ToBase64String(memoryStream.ToArray());
        var mimeType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType;

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
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64File
                            }
                        },
                        new
                        {
                            text = BuildPrompt()
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
            "Extracting resume fields with Gemini for file {FileName}. Model: {Model}, MaxRetryAttempts: {MaxRetryAttempts}",
            fileName,
            _options.Model,
            maxRetryAttempts);

        for (var attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("x-goog-api-key", _options.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ParseExtractionResponse(responseBody);
                }

                if (IsTransientStatusCode(response.StatusCode))
                {
                    if (attempt == maxRetryAttempts)
                    {
                        _logger.LogInformation(
                            "Gemini extraction remained unavailable after {AttemptCount} attempts for file {FileName}. Status: {StatusCode}, Body: {ResponseBody}",
                            attempt,
                            fileName,
                            (int)response.StatusCode,
                            Truncate(responseBody));

                        throw new ResumeExtractionUnavailableException(
                            "Resume extraction is temporarily unavailable. Please try again shortly.");
                    }

                    var retryDelay = GetRetryDelay(attempt, initialRetryDelayMilliseconds);

                    _logger.LogInformation(
                        "Transient Gemini extraction failure on attempt {Attempt}/{MaxRetryAttempts} for file {FileName}. Status: {StatusCode}. Retrying in {RetryDelayMs}ms.",
                        attempt,
                        maxRetryAttempts,
                        fileName,
                        (int)response.StatusCode,
                        retryDelay.TotalMilliseconds);

                    await Task.Delay(retryDelay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Gemini extraction failed with status {(int)response.StatusCode}: {responseBody}");
            }
            catch (HttpRequestException ex) when (attempt < maxRetryAttempts)
            {
                var retryDelay = GetRetryDelay(attempt, initialRetryDelayMilliseconds);

                _logger.LogInformation(
                    ex,
                    "Network error during Gemini extraction on attempt {Attempt}/{MaxRetryAttempts} for file {FileName}. Retrying in {RetryDelayMs}ms.",
                    attempt,
                    maxRetryAttempts,
                    fileName,
                    retryDelay.TotalMilliseconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation(
                    ex,
                    "Gemini extraction failed because the upstream service could not be reached for file {FileName}.",
                    fileName);

                throw new ResumeExtractionUnavailableException(
                    "Resume extraction is temporarily unavailable. Please try again shortly.");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < maxRetryAttempts)
            {
                var retryDelay = GetRetryDelay(attempt, initialRetryDelayMilliseconds);

                _logger.LogInformation(
                    ex,
                    "Gemini extraction timed out on attempt {Attempt}/{MaxRetryAttempts} for file {FileName}. Retrying in {RetryDelayMs}ms.",
                    attempt,
                    maxRetryAttempts,
                    fileName,
                    retryDelay.TotalMilliseconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    ex,
                    "Gemini extraction timed out for file {FileName}.",
                    fileName);

                throw new ResumeExtractionUnavailableException(
                    "Resume extraction is temporarily unavailable. Please try again shortly.");
            }
        }

        throw new ResumeExtractionUnavailableException(
            "Resume extraction is temporarily unavailable. Please try again shortly.");
    }

    private static ExtractApplicationResumeDto ParseExtractionResponse(string responseBody)
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
            throw new InvalidOperationException("Gemini returned an empty extraction response.");
        }

        var result = JsonSerializer.Deserialize<ExtractApplicationResumeDto>(jsonText, SerializerOptions)
            ?? new ExtractApplicationResumeDto();

        return new ExtractApplicationResumeDto
        {
            Education = NormalizeEducation(result.Education),
            Department = NormalizeDepartment(result.Department),
            CGPA = NormalizeCGPA(result.CGPA),
            LinkedInUrl = NormalizeUrl(result.LinkedInUrl),
            AdditionalUrl = NormalizeUrl(result.AdditionalUrl),
            Location = result.Location,
            Experience = NormalizeExperience(result.Experience)
        };
    }

    private static string BuildPrompt() =>
        "Extract candidate information from this resume.\n" +
        "Return only JSON that matches the provided schema exactly.\n" +
        "Rules:\n" +
        "- Use the exact property names from the schema.\n" +
        "- If a value is missing, unclear, or not explicitly present, return null.\n" +
        "- Do not guess or invent information.\n" +
        "- For LinkedInUrl and AdditionalUrl, always return a full absolute URL including https://.\n" +
        "- For phone numbers, preserve the original formatting.\n" +
        "- For Education, return only the university name.\n" +
        "- For Department, return only the subject, major, or department name.\n" +
        "- For CGPA, return only the CGPAPA/CGPA value such as \"3.85/4.00\" or \"3.85\".\n" +
        "- For Experience, return only the total years of experience as a number-like string such as \"0\", \"1\", \"2\", or \"3.5\".\n" +
        "- Do not include date ranges, job titles, achievements, coursework, or multiple education entries inside Education, Department, CGPA, or Experience.\n";


    private static string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return trimmed;
        }

        var withHttps = $"https://{trimmed.TrimStart('/')}";
        return Uri.TryCreate(withHttps, UriKind.Absolute, out _)
            ? withHttps
            : trimmed;
    }

    private static string? NormalizeEducation(string? value)
    {
        return NormalizeText(value, 255);
    }

    private static string? NormalizeDepartment(string? value)
    {
        return NormalizeText(value, 255);
    }

    private static string? NormalizeCGPA(string? value)
    {
        return NormalizeText(value, 50);
    }

    private static string? NormalizeExperience(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(value, @"\d+(\.\d+)?");
        return match.Success ? match.Value : value.Trim();
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private static object BuildResponseSchema()
    {
        var dtoProperties = typeof(ExtractApplicationResumeDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var propertyNames = dtoProperties.Select(x => x.Name).ToArray();

        var properties = dtoProperties.ToDictionary(
            x => x.Name,
            x => (object)new
            {
                type = new[] { GetJsonSchemaType(x.PropertyType), "null" },
                description = $"Extract {x.Name} from the resume. Return null if unavailable."
            });

        return new
        {
            type = "object",
            properties,
            required = propertyNames,
            additionalProperties = false
        };
    }

    private static string GetJsonSchemaType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType == typeof(int) || actualType == typeof(long))
        {
            return "integer";
        }

        if (actualType == typeof(decimal) || actualType == typeof(double) || actualType == typeof(float))
        {
            return "number";
        }

        if (actualType == typeof(bool))
        {
            return "boolean";
        }

        return "string";
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
}
