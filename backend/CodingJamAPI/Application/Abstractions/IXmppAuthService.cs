namespace CodingJamAPI.Application.Abstractions;

/// <summary>Answers the Jabber server (ejabberd external auth): a Jabber account is a web account.</summary>
public interface IXmppAuthService
{
    Task<bool> ValidateCredentialsAsync(string user, string server, string password, CancellationToken ct);
    Task<bool> UserExistsAsync(string user, string server, CancellationToken ct);
}
