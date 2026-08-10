using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.Identity.Services;

public interface IIdentityEmailSender
{
    Task SendPasswordResetAsync(
        string email,
        string displayName,
        string resetUrl,
        CancellationToken cancellationToken);
}

public sealed partial class IdentityEmailSender(
    IOptions<IdentityEmailOptions> options,
    ILogger<IdentityEmailSender> logger) : IIdentityEmailSender
{
    public async Task SendPasswordResetAsync(
        string email,
        string displayName,
        string resetUrl,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            LogEmailNotConfigured(logger);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "CouchClash Passwort zurücksetzen",
            Body = $"Hallo {displayName},\n\nüber diesen Link kannst du dein Passwort zurücksetzen:\n{resetUrl}\n\nDer Link ist zeitlich begrenzt und kann nach erfolgreicher Nutzung nicht erneut verwendet werden.\n\nWenn du das nicht angefordert hast, kannst du diese E-Mail ignorieren.",
            IsBodyHtml = false
        };
        message.To.Add(email);
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl
        };
        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
        LogResetEmailSent(logger);
    }

    [LoggerMessage(EventId = 2400, Level = LogLevel.Warning,
        Message = "Password reset requested but SMTP email is not configured")]
    private static partial void LogEmailNotConfigured(ILogger logger);

    [LoggerMessage(EventId = 2401, Level = LogLevel.Information,
        Message = "Password reset email sent")]
    private static partial void LogResetEmailSent(ILogger logger);
}
