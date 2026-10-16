using System.Text.Json.Serialization;

namespace Shared.Dtos.Common;

public sealed class ApiErrorResponse
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string Detail { get; init; }
    public string? TraceId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RequiresVerification { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextAction { get; init; }
}
