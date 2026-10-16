namespace Infrastructure.Services.EmailTemplates;

public sealed record RenderedEmailTemplate(
    string Subject,
    string Body,
    bool IsBodyHtml);
