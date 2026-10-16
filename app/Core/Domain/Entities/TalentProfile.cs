namespace Domain.Entities;

public sealed class TalentProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string? ResumeUrl { get; set; }

    public string? ResumeFileName { get; set; }

    public string? PictureUrl { get; set; }

    public string? PictureFileName { get; set; }

    public string? Location { get; set; }

    public string? Tagline { get; set; }

    public string? Bio { get; set; }

    public string? Linkedin { get; set; }

    public string? AdditionalUrl { get; set; }

    public string? VideoIntro { get; set; }

    public int Experience { get; set; }

    public string? Workplaces { get; set; }

    public decimal ExpectedSalary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<TalentProfileSkill> Skills { get; set; } = new List<TalentProfileSkill>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
}
