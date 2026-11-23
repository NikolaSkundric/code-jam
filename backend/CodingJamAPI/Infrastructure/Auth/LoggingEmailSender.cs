using CodingJamAPI.Application.Abstractions;

namespace CodingJamAPI.Infrastructure.Auth;

/// <summary>
/// Development email sender: writes the message to the log instead of sending it.
/// Replace with a real SMTP/provider implementation before production.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Password reset requested for {Email}. Reset link: {Link}", toEmail, resetLink);
        return Task.CompletedTask;
    }
}
