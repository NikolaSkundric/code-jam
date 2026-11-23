namespace CodingJamAPI.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Verifies a password; when hash is null it still burns equivalent time and returns false.</summary>
    bool Verify(string password, string? hash);
}
