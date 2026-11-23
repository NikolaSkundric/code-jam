using System.Text;
using System.Text.Json;

namespace CodingJamAPI.Infrastructure.Xmpp;

/// <summary>HTTP access to the internal management API of an ejabberd server (compose network only).</summary>
public sealed class EjabberdApiClient(HttpClient http)
{
    /// <summary>Runs an ejabberd command (POST {root}/api/{command}) and returns the JSON result.</summary>
    public async Task<JsonElement> CommandAsync(string root, string command, object body, CancellationToken ct)
    {
        // StringContent has a Content-Length; ejabberd answers 400 to the chunked body PostAsJsonAsync would send.
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"{root.TrimEnd('/')}/api/{command}", content, ct);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return document.RootElement.Clone();
    }

    public Task<string> GetTextAsync(string url, CancellationToken ct) => http.GetStringAsync(url, ct);
}
