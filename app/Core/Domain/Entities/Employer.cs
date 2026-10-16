namespace Domain.Entities;

public sealed class Employer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
