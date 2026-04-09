using System;
using System.IO;
using System.Threading.Tasks;
using KLC.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KLC.Files;

/// <summary>
/// Local filesystem file upload service for development.
/// </summary>
public class LocalFileUploadService : IFileUploadService
{
    private readonly ILogger<LocalFileUploadService> _logger;
    private readonly string _uploadRoot;
    private readonly string? _publicBaseUrl;

    public LocalFileUploadService(
        ILogger<LocalFileUploadService> logger,
        IOptions<FileStorageSettings> settings)
    {
        _logger = logger;
        _uploadRoot = settings.Value.LocalPath;
        _publicBaseUrl = settings.Value.PublicBaseUrl;
    }

    public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string folder)
    {
        var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var folderPath = Path.Combine(_uploadRoot, folder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, safeFileName);
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        var url = !string.IsNullOrEmpty(_publicBaseUrl)
            ? $"{_publicBaseUrl.TrimEnd('/')}/{folder}/{safeFileName}"
            : $"/uploads/{folder}/{safeFileName}";

        _logger.LogInformation("[Local] Uploaded: {FileName} -> {Url}", fileName, url);

        return new FileUploadResult { Url = url, FileSize = fileStream.Length };
    }

    public Task DeleteAsync(string fileUrl)
    {
        // Local files: best-effort delete
        try
        {
            var relativePath = fileUrl.Replace("/uploads/", "");
            var filePath = Path.Combine(_uploadRoot, relativePath);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Local] Failed to delete: {Url}", fileUrl);
        }
        return Task.CompletedTask;
    }
}
