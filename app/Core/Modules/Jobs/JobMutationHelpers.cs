using Domain.Entities;

namespace Modules.Jobs;

internal static class JobMutationHelpers
{
    public static void ReplaceSkills(Job job, IReadOnlyList<string> requestedSkills)
    {
        job.Skills.Clear();

        foreach (var (skillName, index) in NormalizeSkills(requestedSkills).Select((value, index) => (value, index)))
        {
            job.Skills.Add(new JobSkill
            {
                Name = skillName,
                DisplayOrder = index
            });
        }
    }

    private static IEnumerable<string> NormalizeSkills(IEnumerable<string> skills)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in skills)
        {
            var normalized = skill.Trim();

            if (normalized.Length == 0 || !seen.Add(normalized))
            {
                continue;
            }

            yield return normalized;
        }
    }
}
