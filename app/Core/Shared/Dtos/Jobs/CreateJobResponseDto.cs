namespace Shared.Dtos.Jobs;

public sealed class CreateJobResponseDto
{
    public Guid JobId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
