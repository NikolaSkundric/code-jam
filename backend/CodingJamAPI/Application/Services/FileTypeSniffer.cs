namespace CodingJamAPI.Application.Services;

/// <summary>
/// Decides whether an upload is a real raster image by looking at its first bytes (the client supplied
/// content type is not trusted). Anything else, including SVG, is treated as a plain file.
/// </summary>
public static class FileTypeSniffer
{
    /// <summary>Returns the image content type, or null if the data is not a supported raster image.</summary>
    public static string? DetectImage(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
        {
            return "image/png";
        }
        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
        {
            return "image/jpeg";
        }
        if (head.Length >= 6 && head[0] == 'G' && head[1] == 'I' && head[2] == 'F' && head[3] == '8')
        {
            return "image/gif";
        }
        if (head.Length >= 12 && head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' &&
            head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P')
        {
            return "image/webp";
        }
        return null;
    }
}
