namespace Modules.Employer.CreateProfile;

public sealed class CreateEmployerProfileRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
