namespace Domain.Entities;

public sealed class JobSkill
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Job Job { get; set; } = null!;
}
