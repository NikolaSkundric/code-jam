namespace CodingJamAPI.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP server. When empty, emails are only written to the log (see LoggingEmailSender).</summary>
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 25;
    public bool UseSsl { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    public string From { get; init; } = "no-reply@chat.local";
    public string FromName { get; init; } = "Chat";
}
