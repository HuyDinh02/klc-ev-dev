using System.IO;
using System.Threading.Tasks;

namespace KLC.Files;

/// <summary>
/// Service for handling file uploads (avatars, station photos, etc.).
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Upload a file and return the public URL.
    /// </summary>
    /// <param name="stream">File content stream.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="folder">Target folder (e.g., "avatars", "stations").</param>
    /// <returns>Public URL of the uploaded file.</returns>
    Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string folder);

    /// <summary>
    /// Delete a previously uploaded file.
    /// </summary>
    /// <param name="fileUrl">The URL returned by UploadAsync.</param>
    Task DeleteAsync(string fileUrl);
}

public class FileUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
}
