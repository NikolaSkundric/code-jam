using System.Net;
using System.Net.Mail;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Auth;

/// <summary>
/// Sends mail through an SMTP server (docker compose ships Mailpit as a local catch-all inbox).
/// Sending runs in the background and failures are only logged, so the forgot-password response
/// never depends on (or reveals anything about) mail delivery.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var body =
            "Hello,\n\n" +
            "Someone asked to reset the password of your chat account. Use this link to choose a new password " +
            "(it works once and expires in 1 hour):\n\n" +
            resetLink + "\n\n" +
            "If this was not you, you can ignore this email: your password stays the same.\n";

        _ = Task.Run(() => SendAsync(toEmail, "Reset your password", body), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var message = new MailMessage(new MailAddress(_options.From, _options.FromName), new MailAddress(toEmail))
            {
                Subject = subject,
                Body = body
            };

            using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
            if (!string.IsNullOrEmpty(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not send the password reset email");
        }
    }
}
