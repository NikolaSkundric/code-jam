using CodingJamAPI.Application.Abstractions;

namespace CodingJamAPI.Infrastructure.Auth;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Used to spend the same time on unknown accounts as on real ones.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.EnhancedHashPassword("dummy-password", WorkFactor);

    public string Hash(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);

    public bool Verify(string password, string? hash)
    {
        if (hash is null)
        {
            BCrypt.Net.BCrypt.EnhancedVerify(password, DummyHash);
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
