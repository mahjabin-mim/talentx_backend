namespace Shared.Dtos.Jobs;

public sealed class GenerateJobDescriptionResponseDto
{
    public string Description { get; init; } = string.Empty;
    public string? Message { get; init; }
}
