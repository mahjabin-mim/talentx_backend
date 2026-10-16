using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid TalentId { get; set; }
    public Guid JobId { get; set; }
    public string? Name { get; set; }
    public decimal ExpectedSalary { get; set; }
    public string? Email { get; set; }
    public int Experience { get; set; }
    public string? Location { get; set; }
    public string? Education { get; set; }
    public string? Department { get; set; }
    public string? CGPA { get; set; }
    public string? ResumeUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? AdditionalUrl { get; set; }
    public string? CoverLetter { get; set; }
    public string? ContactNumber { get; set; }
    public ApplicationStatus Status { get; set; } 
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Job Job { get; set; } = null!;
    public TalentProfile Talent { get; set; } = null!;

}
