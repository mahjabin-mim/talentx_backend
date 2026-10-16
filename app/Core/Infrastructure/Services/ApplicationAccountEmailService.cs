using System.Net;
using System.Net.Mail;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class ApplicationAccountEmailService(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<ApplicationAccountEmailService> logger) : IApplicationAccountEmailService
{
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;
    private readonly ILogger<ApplicationAccountEmailService> _logger = logger;

    public async Task SendNewAccountPasswordAsync(string email, string? fullName, string password)
    {
        var host = _smtpOptions.Host?.Trim() ?? string.Empty;
        var fromEmail = _smtpOptions.FromEmail?.Trim() ?? string.Empty;
        var username = _smtpOptions.Username?.Trim() ?? string.Empty;
        var smtpPassword = NormalizePassword(host, _smtpOptions.Password);

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogInformation(
                "SMTP is not configured. Generated password for {Email}: {Password}",
                email,
                password);

            return;
        }

        using var client = new SmtpClient(host, _smtpOptions.Port)
        {
            EnableSsl = _smtpOptions.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, smtpPassword);
        }

        var recipientName = string.IsNullOrWhiteSpace(fullName)
            ? "there"
            : fullName.Trim();

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _smtpOptions.FromName),
            Subject = "Your TalentX temporary password",
            Body =
                $"Hello {recipientName},{Environment.NewLine}{Environment.NewLine}" +
                $"Your user id is {email}.{Environment.NewLine}" +
                $"Your temporary password is {password}{Environment.NewLine}{Environment.NewLine}" +
                "Please sign in and change this password as soon as possible.",
            IsBodyHtml = false
        };

        message.To.Add(email);

        try
        {
            await client.SendMailAsync(message);
        }
        catch (SmtpException ex)
        {
            _logger.LogInformation(
                ex,
                "SMTP send failed for {Email}. Host: {Host}, UsernameConfigured: {HasUsername}, FromEmailConfigured: {HasFromEmail}",
                email,
                host,
                !string.IsNullOrWhiteSpace(username),
                !string.IsNullOrWhiteSpace(fromEmail));

            var providerHint = host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
                ? "Check that you are using a Gmail App Password, not your normal Gmail password, and paste the 16-character app password without spaces."
                : "Check the SMTP host, port, username, password, and SSL settings.";

            var detail = ex.InnerException?.Message ?? ex.Message;

            throw new InvalidOperationException($"SMTP send failed. {detail} {providerHint}", ex);
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
