using System.Reflection;

namespace Infrastructure.Services.EmailTemplates;

public sealed class EmailTemplateRenderer
{
    private const string ResourceRoot = "Infrastructure.EmailTemplates";

    public RenderedEmailTemplate Render(
        string templateName,
        IReadOnlyDictionary<string, string> values,
        bool isBodyHtml = false)
    {
        var subject = RenderResource($"{ResourceRoot}.{templateName}.subject.txt", values);
        var body = RenderResource($"{ResourceRoot}.{templateName}.body.txt", values);

        return new RenderedEmailTemplate(subject.Trim(), body, isBodyHtml);
    }

    private static string RenderResource(string resourceName, IReadOnlyDictionary<string, string> values)
    {
        var template = ReadResource(resourceName);

        foreach (var value in values)
        {
            template = template.Replace(
                $"{{{{{value.Key}}}}}",
                value.Value,
                StringComparison.Ordinal);
        }

        return template;
    }

    private static string ReadResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Email template resource '{resourceName}' was not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
