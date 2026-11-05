namespace WhiteboardSuggestion.Core;

/// <summary>
/// Resolves common image extensions to MIME types accepted by the demo.
/// </summary>
public static class ImageMimeTypeResolver
{
    public static string GetMimeType(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("An image file path is required.", nameof(filePath));
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => throw new NotSupportedException(
                $"The image type '{extension}' is not supported. Use PNG, JPG, JPEG, or WEBP.")
        };
    }
}
