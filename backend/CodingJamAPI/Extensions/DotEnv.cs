namespace CodingJamAPI.Extensions;

/// <summary>
/// Reads the repository .env file (the same one docker compose uses) so that running the API directly
/// (dotnet run, Visual Studio, dotnet ef) works without copying secrets into appsettings files.
/// Only used for local development; containers get the values from compose environment variables.
/// </summary>
public static class DotEnv
{
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 5440; // host port published by docker-compose for postgres

    /// <summary>Finds .env in the start directory or one of its parents; empty when there is none.</summary>
    public static IReadOnlyDictionary<string, string> Read(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (File.Exists(path))
            {
                return Parse(File.ReadAllLines(path));
            }
        }
        return new Dictionary<string, string>();
    }

    /// <summary>Connection string for a postgres reachable from the host (the compose published port).</summary>
    public static string? HostConnectionString(IReadOnlyDictionary<string, string> env) =>
        env.TryGetValue("POSTGRES_PASSWORD", out var password) && password.Length > 0
            ? $"Host={DefaultHost};Port={DefaultPort};Database=chatdb;Username=postgres;Password={password}"
            : null;

    private static Dictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var index = line.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }

            var value = line[(index + 1)..].Trim();
            if (value.Length >= 2 && value[0] == value[^1] && (value[0] == '"' || value[0] == '\''))
            {
                value = value[1..^1];
            }
            result[line[..index].Trim()] = value;
        }
        return result;
    }
}
