namespace CodingJamAPI.Application.Abstractions;

/// <summary>Stores uploaded files under server-generated keys (never user-supplied names).</summary>
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken ct);
    Stream OpenRead(string key);
    void Delete(string key);

    /// <summary>Permanently removes every file under a key prefix (all files of a deleted room).</summary>
    void DeleteFolder(string prefix);
}
