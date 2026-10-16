using Domain.Enums;

namespace Domain.Entities;

public sealed class Job
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public WorkArrangement WorkArrangement { get; set; }
    public ExperienceLevel ExperienceLevel { get; set; }
    public JobStatus Status { get; set; }
    public int ExperienceMinYears { get; set; }
    public int ExperienceMaxYears { get; set; }
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public int NumberOfPositions { get; set; }
    public DateTimeOffset ApplicationDeadline { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
    public bool Deleted { get; set; }

    public Employer Employer { get; set; } = null!;
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<JobSkill> Skills { get; set; } = new List<JobSkill>();

}
