using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.AiScoring;

public sealed class AiScoreCalculationService(
    AppDbContext dbContext,
    HttpClient httpClient,
    IResumeStorageService resumeStorageService,
    IOptions<GeminiOptions> geminiOptions,
    IOptions<AiScoringOptions> aiScoringOptions,
    ILogger<AiScoreCalculationService> logger) : IAiScoreCalculationService
{
    private static readonly Regex NonAlphaNumericRegex = new("[^a-z0-9+#]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "in",
        "is", "it", "of", "on", "or", "that", "the", "to", "with", "you",
        "your", "our", "we", "will", "this", "these", "those", "using",
        "use", "used", "can", "have", "has", "had", "into", "their", "them"
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _dbContext = dbContext;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IResumeStorageService _resumeStorageService = resumeStorageService;
    private readonly GeminiOptions _geminiOptions = geminiOptions.Value;
    private readonly AiScoringOptions _aiScoringOptions = aiScoringOptions.Value;
    private readonly ILogger<AiScoreCalculationService> _logger = logger;

    public async Task ProcessPendingScoresAsync(int maxBatchSize, CancellationToken cancellationToken = default)
    {
        if (maxBatchSize <= 0)
        {
            return;
        }

        var queuedItems = await _dbContext.AiScoreRefreshQueues
            .OrderBy(x => x.RequestedAt)
            .Take(maxBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var queuedItem in queuedItems)
        {
            try
            {
                await CalculateAndSaveScoreAsync(queuedItem, cancellationToken);
                _dbContext.AiScoreRefreshQueues.Remove(queuedItem);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                queuedItem.AttemptCount += 1;
                queuedItem.LastAttemptedAt = DateTimeOffset.UtcNow;
                queuedItem.LastError = Truncate(ex.Message, 1000);

                _logger.LogWarning(
                    ex,
                    "AI score calculation failed for JobId: {JobId}, TalentId: {TalentId}. Reason: {Reason}",
                    queuedItem.JobId,
                    queuedItem.TalentId,
                    queuedItem.Reason);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task CalculateAndSaveScoreAsync(
        AiScoreRefreshQueue queuedItem,
        CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.Id == queuedItem.JobId && !x.Deleted, cancellationToken);

        var talent = await _dbContext.TalentProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(
                x => x.Id == queuedItem.TalentId && x.User.IsActive && !x.User.Deleted,
                cancellationToken);

        if (job is null || talent is null)
        {
            _logger.LogInformation(
                "Skipping AI score refresh because the job or talent no longer exists. JobId: {JobId}, TalentId: {TalentId}",
                queuedItem.JobId,
                queuedItem.TalentId);

            return;
        }

        var latestApplication = await _dbContext.Applications
            .AsNoTracking()
            .Where(x => x.JobId == queuedItem.JobId && x.TalentId == queuedItem.TalentId)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var heuristicScore = CalculateHeuristicScore(job, talent, latestApplication);
        AiScoreResult? geminiResult = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_geminiOptions.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            geminiResult = await GenerateScoreAsync(job, talent, latestApplication, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _aiScoringOptions.UseHeuristicFallbackWhenAiUnavailable)
        {
            _logger.LogWarning(
                ex,
                "Gemini scoring unavailable. Falling back to heuristic score only. JobId: {JobId}, TalentId: {TalentId}",
                queuedItem.JobId,
                queuedItem.TalentId);
        }

        if (geminiResult is null && !_aiScoringOptions.UseHeuristicFallbackWhenAiUnavailable)
        {
            throw new InvalidOperationException("Gemini scoring failed and heuristic fallback is disabled.");
        }

        var finalScore = BlendScores(heuristicScore, geminiResult?.Score);
        var now = DateTimeOffset.UtcNow;

        var existingScore = await _dbContext.AiScores
            .FirstOrDefaultAsync(
                x => x.JobId == queuedItem.JobId && x.TalentId == queuedItem.TalentId,
                cancellationToken);

        if (existingScore is null)
        {
            _dbContext.AiScores.Add(new AiScore
            {
                JobId = queuedItem.JobId,
                TalentId = queuedItem.TalentId,
                Score = finalScore,
                CalculatedAt = now
            });
        }
        else
        {
            existingScore.Score = finalScore;
            existingScore.CalculatedAt = now;
        }

        _logger.LogInformation(
            "AI score updated. JobId: {JobId}, TalentId: {TalentId}, FinalScore: {FinalScore}, HeuristicScore: {HeuristicScore}, GeminiScore: {GeminiScore}",
            queuedItem.JobId,
            queuedItem.TalentId,
            finalScore,
            heuristicScore,
            geminiResult?.Score);
    }

    private async Task<AiScoreResult> GenerateScoreAsync(
        Job job,
        TalentProfile talent,
        Application? latestApplication,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPrompt(job, talent, latestApplication);
        var parts = new List<object>();

        var resumeUrl = latestApplication?.ResumeUrl ?? talent.ResumeUrl;
        if (!string.IsNullOrWhiteSpace(resumeUrl))
        {
            var downloadedResume = await _resumeStorageService.DownloadByUrlAsync(resumeUrl, cancellationToken);
            await using var resumeStream = downloadedResume.Stream;
            await using var memoryStream = new MemoryStream();
            await resumeStream.CopyToAsync(memoryStream, cancellationToken);

            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = string.IsNullOrWhiteSpace(downloadedResume.ContentType)
                        ? "application/pdf"
                        : downloadedResume.ContentType,
                    data = Convert.ToBase64String(memoryStream.ToArray())
                }
            });
        }

        parts.Add(new
        {
            text = prompt
        });

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = parts.ToArray()
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseJsonSchema = BuildResponseSchema()
            }
        };

        var endpoint = $"{_geminiOptions.BaseUrl.TrimEnd('/')}/v1beta/models/{_geminiOptions.Model}:generateContent";
        var maxRetryAttempts = Math.Max(1, _geminiOptions.MaxRetryAttempts);
        var initialRetryDelayMilliseconds = Math.Max(250, _geminiOptions.InitialRetryDelayMilliseconds);

        for (var attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("x-goog-api-key", _geminiOptions.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ParseScoreResponse(responseBody);
                }

                if (IsTransientStatusCode(response.StatusCode))
                {
                    if (attempt == maxRetryAttempts)
                    {
                        throw new InvalidOperationException(
                            $"Gemini AI scoring remained unavailable after {attempt} attempts. Status: {(int)response.StatusCode}. Body: {Truncate(responseBody)}");
                    }

                    await Task.Delay(GetRetryDelay(attempt, initialRetryDelayMilliseconds), cancellationToken);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Gemini AI scoring failed with status {(int)response.StatusCode}: {responseBody}");
            }
            catch (HttpRequestException) when (attempt < maxRetryAttempts)
            {
                await Task.Delay(GetRetryDelay(attempt, initialRetryDelayMilliseconds), cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < maxRetryAttempts)
            {
                await Task.Delay(GetRetryDelay(attempt, initialRetryDelayMilliseconds), cancellationToken);
            }
        }

        throw new InvalidOperationException("Gemini AI scoring could not complete.");
    }

    private static string BuildPrompt(Job job, TalentProfile talent, Application? latestApplication)
    {
        var location = job.WorkArrangement == WorkArrangement.Remote
            ? $"Remote. Preferred timezone: {NullIfEmpty(job.Timezone) ?? "Not specified"}"
            : $"{NullIfEmpty(job.City) ?? "Unknown city"}, {NullIfEmpty(job.CountryCode) ?? "Unknown country"}";

        var jobSkills = job.Skills
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.Name)
            .ToList();

        var talentSkills = talent.Skills
            .OrderBy(x => x.DisplayOrder)
            .Select(x => $"{FormatTalentSkillName(x.Language)} ({x.YearsOfExperience} years)")
            .ToList();

        var expectedSalaryText = talent.ExpectedSalary > 0
            ? $"{talent.ExpectedSalary:0.##}"
            : "Not specified";

        var applicationExpectedSalaryText = latestApplication?.ExpectedSalary > 0
            ? latestApplication.ExpectedSalary.ToString("0.##")
            : "Not specified";

        var applicationExperienceText = latestApplication is null
            ? "Not specified"
            : $"{latestApplication.Experience:0.##}";

        return
            "You are scoring how well a talent matches a job opening.\n" +
            "Return only JSON that matches the provided schema exactly.\n" +
            "Score from 0 to 100.\n" +
            "Use only evidence from the job details, talent profile, application context, and attached resume if provided.\n" +
            "The most important instruction is this: match the talent resume, talent tagline, talent skills, years of experience, and expected salary against the job title, job description, responsibilities, required skills, experience range, work arrangement, location, employment type, and salary range.\n" +
            "Scoring priorities:\n" +
            "- Resume to job title/details/responsibilities match: 30 points\n" +
            "- Talent skills to job required skills match: 25 points\n" +
            "- Talent tagline and bio to job title/details match: 15 points\n" +
            "- Talent years of experience to job experience level and required range match: 15 points\n" +
            "- Expected salary to job salary range match: 10 points\n" +
            "- Other job fields alignment such as work arrangement, location, employment type, and application evidence: 5 points\n" +
            "Rules:\n" +
            "- Strongly reward direct keyword and concept overlap between the resume and the job title/details.\n" +
            "- Strongly reward overlap between the talent skills and the job skills.\n" +
            "- Compare the talent's experience years and seniority with the job's expected experience range and level.\n" +
            "- Compare expected salary with the job salary range. If the expected salary is much higher than the job range, lower the score.\n" +
            "- Use job other fields too, including work arrangement, location, employment type, and status, but they are secondary to the core skills/resume match.\n" +
            "- Penalize missing, weak, or contradictory evidence.\n" +
            "- Do not guess or invent facts.\n\n" +
            "JOB DETAILS\n" +
            $"Title: {job.Title}\n" +
            $"Description: {job.Description}\n" +
            $"Responsibilities: {job.Responsibilities}\n" +
            $"Skills: {string.Join(", ", jobSkills)}\n" +
            $"Employment type: {job.EmploymentType}\n" +
            $"Work arrangement: {job.WorkArrangement}\n" +
            $"Location: {location}\n" +
            $"Experience level: {job.ExperienceLevel}\n" +
            $"Experience range: {job.ExperienceMinYears}-{job.ExperienceMaxYears} years\n" +
            $"Salary range: {job.SalaryMin}-{job.SalaryMax} {job.CurrencyCode}\n" +
            $"Number of positions: {job.NumberOfPositions}\n" +
            $"Application deadline: {job.ApplicationDeadline:O}\n" +
            $"Status: {job.Status}\n\n" +
            "TALENT PROFILE\n" +
            $"Name: {NullIfEmpty(talent.User.Fullname) ?? "Not specified"}\n" +
            $"Tagline: {NullIfEmpty(talent.Tagline) ?? "Not specified"}\n" +
            $"Bio: {NullIfEmpty(talent.Bio) ?? "Not specified"}\n" +
            $"Location: {NullIfEmpty(talent.Location) ?? "Not specified"}\n" +
            $"Skills: {string.Join(", ", talentSkills)}\n" +
            $"Experience: {talent.Experience} years\n" +
            $"Workplaces: {NullIfEmpty(talent.Workplaces) ?? "Not specified"}\n" +
            $"Expected salary: {expectedSalaryText}\n" +
            $"LinkedIn: {NullIfEmpty(talent.Linkedin) ?? "Not specified"}\n" +
            $"Additional URL: {NullIfEmpty(talent.AdditionalUrl) ?? "Not specified"}\n\n" +
            "APPLICATION CONTEXT\n" +
            $"Applied: {(latestApplication is null ? "No" : "Yes")}\n" +
            $"Application experience: {applicationExperienceText}\n" +
            $"Application expected salary: {applicationExpectedSalaryText}\n" +
            $"Application education: {NullIfEmpty(latestApplication?.Education) ?? "Not specified"}\n" +
            $"Application department: {NullIfEmpty(latestApplication?.Department) ?? "Not specified"}\n" +
            $"Application cover letter: {NullIfEmpty(latestApplication?.CoverLetter) ?? "Not specified"}\n" +
            "If a resume is attached in the request, use it as primary evidence before deciding the final score.\n" +
            "In the summary, briefly mention the strongest match or biggest mismatch.\n";
    }

    private static object BuildResponseSchema() =>
        new
        {
            type = "object",
            properties = new
            {
                score = new
                {
                    type = "number",
                    description = "A match score from 0 to 100."
                },
                summary = new
                {
                    type = "string",
                    description = "A brief reason for the score."
                }
            },
            required = new[] { "score", "summary" },
            additionalProperties = false
        };

    private static AiScoreResult ParseScoreResponse(string responseBody)
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
            throw new InvalidOperationException("Gemini returned an empty AI scoring response.");
        }

        var result = JsonSerializer.Deserialize<AiScoreResult>(jsonText, SerializerOptions)
            ?? throw new InvalidOperationException("Gemini returned an invalid AI scoring payload.");

        return result with
        {
            Score = decimal.Round(decimal.Clamp(result.Score, 0m, 100m), 2)
        };
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

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private decimal CalculateHeuristicScore(
        Job job,
        TalentProfile talent,
        Application? latestApplication)
    {
        var skillScore = CalculateSkillScore(job, talent, latestApplication);
        var titleAndDetailsScore = CalculateTitleAndDetailsScore(job, talent, latestApplication);
        var experienceScore = CalculateExperienceScore(job, talent, latestApplication);
        var salaryScore = CalculateSalaryScore(job, talent, latestApplication);
        var fieldAlignmentScore = CalculateFieldAlignmentScore(job, talent, latestApplication);

        return decimal.Round(
            decimal.Clamp(
                skillScore + titleAndDetailsScore + experienceScore + salaryScore + fieldAlignmentScore,
                0m,
                100m),
            2);
    }

    private static decimal CalculateSkillScore(
        Job job,
        TalentProfile talent,
        Application? latestApplication)
    {
        var jobSkills = job.Skills
            .Select(x => NormalizePhrase(x.Name))
            .Where(x => x.Length > 0)
            .Distinct()
            .ToList();

        if (jobSkills.Count == 0)
        {
            return 20m;
        }

        var talentSkillSet = talent.Skills
            .Select(x => NormalizePhrase(FormatTalentSkillName(x.Language)))
            .Where(x => x.Length > 0)
            .ToHashSet();

        var talentText = string.Join(
            " ",
            new string?[]
            {
                talent.Tagline,
                talent.Bio,
                talent.Workplaces,
                latestApplication?.CoverLetter,
                latestApplication?.Education,
                latestApplication?.Department
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var matchedSkills = 0;

        foreach (var jobSkill in jobSkills)
        {
            if (talentSkillSet.Contains(jobSkill) ||
                ContainsNormalizedPhrase(talentText, jobSkill))
            {
                matchedSkills += 1;
            }
        }

        var ratio = (decimal)matchedSkills / jobSkills.Count;
        return decimal.Round(ratio * 35m, 2);
    }

    private static decimal CalculateTitleAndDetailsScore(
        Job job,
        TalentProfile talent,
        Application? latestApplication)
    {
        var jobText = string.Join(
            " ",
            new[]
            {
                job.Title,
                job.Description,
                job.Responsibilities,
                string.Join(" ", job.Skills.Select(x => x.Name))
            });

        var talentText = string.Join(
            " ",
            new string?[]
            {
                talent.Tagline,
                talent.Bio,
                talent.Workplaces,
                latestApplication?.CoverLetter,
                latestApplication?.Education,
                latestApplication?.Department
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var overlap = CalculateTokenOverlap(jobText, talentText);
        return decimal.Round(overlap * 30m, 2);
    }

    private static decimal CalculateExperienceScore(
        Job job,
        TalentProfile talent,
        Application? latestApplication)
    {
        var years = latestApplication?.Experience > 0
            ? latestApplication.Experience
            : talent.Experience;

        if (years <= 0)
        {
            return 6m;
        }

        if (years >= job.ExperienceMinYears && years <= job.ExperienceMaxYears)
        {
            return 20m;
        }

        if (years > job.ExperienceMaxYears)
        {
            return 17m;
        }

        var gap = job.ExperienceMinYears - years;
        return gap switch
        {
            <= 1 => 14m,
            <= 2 => 10m,
            <= 3 => 6m,
            _ => 2m
        };
    }

    private static decimal CalculateSalaryScore(
        Job job,
        TalentProfile talent,
        Application? latestApplication)
    {
        var expectedSalary = latestApplication?.ExpectedSalary > 0
            ? latestApplication.ExpectedSalary
            : talent.ExpectedSalary;

        if (expectedSalary <= 0 || job.SalaryMin <= 0 || job.SalaryMax <= 0)
        {
            return 8m;
        }

        if (expectedSalary >= job.SalaryMin && expectedSalary <= job.SalaryMax)
        {
            return 15m;
        }

        if (expectedSalary < job.SalaryMin)
        {
            return 11m;
        }

        var overshootRatio = job.SalaryMax == 0
            ? 1m
            : (expectedSalary - job.SalaryMax) / job.SalaryMax;

        return overshootRatio switch
        {
            <= 0.10m => 11m,
            <= 0.20m => 8m,
            <= 0.35m => 4m,
            _ => 0m
        };
    }

    private static decimal CalculateFieldAlignmentScore(
        Job job,
        TalentProfile talent,
        Application? latestApplication)
    {
        decimal score = 0m;

        if (job.WorkArrangement == WorkArrangement.Remote)
        {
            score += 4m;
        }
        else
        {
            var locationText = string.Join(
                " ",
                new string?[] { talent.Location, latestApplication?.Location }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (ContainsNormalizedPhrase(locationText, job.City) ||
                ContainsNormalizedPhrase(locationText, job.CountryCode))
            {
                score += 4m;
            }
            else if (!string.IsNullOrWhiteSpace(locationText))
            {
                score += 2m;
            }
        }

        if (!string.IsNullOrWhiteSpace(latestApplication?.CoverLetter))
        {
            score += 2m;
        }

        if (!string.IsNullOrWhiteSpace(talent.Tagline))
        {
            score += 2m;
        }

        if (talent.Skills.Count > 0)
        {
            score += 2m;
        }

        return decimal.Round(decimal.Clamp(score, 0m, 10m), 2);
    }

    private decimal BlendScores(decimal heuristicScore, decimal? geminiScore)
    {
        if (!geminiScore.HasValue)
        {
            return decimal.Round(decimal.Clamp(heuristicScore, 0m, 100m), 2);
        }

        var heuristicWeight = decimal.Clamp(_aiScoringOptions.HeuristicWeight, 0m, 1m);
        var aiWeight = decimal.Clamp(_aiScoringOptions.AiWeight, 0m, 1m);
        var totalWeight = heuristicWeight + aiWeight;

        if (totalWeight <= 0)
        {
            heuristicWeight = 0.65m;
            aiWeight = 0.35m;
            totalWeight = 1m;
        }

        var normalizedHeuristicWeight = heuristicWeight / totalWeight;
        var normalizedAiWeight = aiWeight / totalWeight;

        return decimal.Round(
            decimal.Clamp(
                (heuristicScore * normalizedHeuristicWeight) + (geminiScore.Value * normalizedAiWeight),
                0m,
                100m),
            2);
    }

    private static decimal CalculateTokenOverlap(string leftText, string rightText)
    {
        var leftTokens = Tokenize(leftText);
        var rightTokens = Tokenize(rightText);

        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0m;
        }

        var overlapCount = leftTokens.Intersect(rightTokens).Count();
        return (decimal)overlapCount / leftTokens.Count;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var cleaned = NonAlphaNumericRegex.Replace(text.ToLowerInvariant(), " ");
        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1 && !StopWords.Contains(x))
            .ToHashSet();

        return tokens;
    }

    private static bool ContainsNormalizedPhrase(string? sourceText, string? phrase)
    {
        var normalizedPhrase = NormalizePhrase(phrase);
        if (string.IsNullOrWhiteSpace(sourceText) || normalizedPhrase.Length == 0)
        {
            return false;
        }

        return NormalizePhrase(sourceText).Contains(normalizedPhrase, StringComparison.Ordinal);
    }

    private static string NormalizePhrase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = NonAlphaNumericRegex.Replace(value.ToLowerInvariant(), " ");
        var parts = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', parts);
    }

    private static string FormatTalentSkillName(TalentSkillLanguage skill)
    {
        return skill switch
        {
            TalentSkillLanguage.CSharp => "C#",
            TalentSkillLanguage.CPlusPlus => "C++",
            TalentSkillLanguage.Sql => "SQL",
            TalentSkillLanguage.Php => "PHP",
            _ => skill.ToString()
        };
    }

    private static string Truncate(string value, int maxLength = 500)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..maxLength]}...";
    }

    private sealed record AiScoreResult(decimal Score, string Summary);
}
