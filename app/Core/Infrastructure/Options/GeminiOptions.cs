namespace Infrastructure.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-2.5-flash";
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com";
    public int MaxRetryAttempts { get; init; } = 3;
    public int InitialRetryDelayMilliseconds { get; init; } = 750;
}
