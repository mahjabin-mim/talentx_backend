namespace Shared.Dtos.Applications;

public sealed class CreateApplicationDto
{
    public bool ExistingUserFound { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string ContactNumber { get; init; } = string.Empty;

    public string Education { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public string CGPA { get; init; } = string.Empty;

    public string LinkedInUrl { get; init; } = string.Empty;

    public string AdditionalUrl { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public string CoverLetter { get; init; } = string.Empty;

    public decimal ExpectedSalary { get; init; }

    public int Experience { get; init; }
}
