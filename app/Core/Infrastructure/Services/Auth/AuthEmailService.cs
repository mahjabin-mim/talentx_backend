using System.Globalization;
using System.Net;
using System.Net.Mail;
using Infrastructure.Options;
using Infrastructure.Services.EmailTemplates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Services;

namespace Infrastructure.Services.Auth;

public sealed class AuthEmailService(
    IOptions<SmtpOptions> smtpOptions,
    EmailTemplateRenderer templateRenderer,
    ILogger<AuthEmailService> logger) : IAuthEmailService
{
    private const string VerificationOtpTemplate = "Auth.EmailVerificationOtp";

    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;
    private readonly EmailTemplateRenderer _templateRenderer = templateRenderer;
    private readonly ILogger<AuthEmailService> _logger = logger;

    public async Task SendEmailVerificationOtpAsync(
        string email,
        string fullName,
        string otpCode,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var template = _templateRenderer.Render(
            VerificationOtpTemplate,
            new Dictionary<string, string>
            {
                ["FullName"] = fullName.Trim(),
                ["OtpCode"] = otpCode,
                ["ExpiresAtUtc"] = expiresAtUtc
                    .ToUniversalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
            });

        await SendAsync(email, template.Subject, template.Body, template.IsBodyHtml, cancellationToken);
    }

    private async Task SendAsync(
        string email,
        string subject,
        string body,
        bool isBodyHtml,
        CancellationToken cancellationToken)
    {
        var host = _smtpOptions.Host?.Trim() ?? string.Empty;
        var fromEmail = _smtpOptions.FromEmail?.Trim() ?? string.Empty;
        var username = _smtpOptions.Username?.Trim() ?? string.Empty;
        var smtpPassword = NormalizePassword(host, _smtpOptions.Password);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP is not configured for auth email delivery.");
        }

        using var client = new SmtpClient(host, _smtpOptions.Port)
        {
            EnableSsl = _smtpOptions.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, smtpPassword);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _smtpOptions.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isBodyHtml
        };

        message.To.Add(email);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            _logger.LogInformation(
                ex,
                "SMTP send failed for auth email. Email: {Email}, Host: {Host}, UsernameConfigured: {HasUsername}, FromEmailConfigured: {HasFromEmail}",
                email,
                host,
                !string.IsNullOrWhiteSpace(username),
                !string.IsNullOrWhiteSpace(fromEmail));

            throw new InvalidOperationException("SMTP send failed for auth email delivery.", ex);
        }
    }

    private static string NormalizePassword(string host, string? password)
    {
        var value = password?.Trim() ?? string.Empty;

        if (host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            return value.Replace(" ", string.Empty, StringComparison.Ordinal);
        }

        return value;
    }
}
