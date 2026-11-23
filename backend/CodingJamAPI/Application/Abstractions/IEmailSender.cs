namespace CodingJamAPI.Application.Abstractions;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
}
