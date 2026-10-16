namespace Shared.Dtos.Jobs;

public sealed class GetJobApplicantsRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
}
