using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Files;

/// <summary>
/// Google Cloud Storage file upload service.
/// Uploads files to a GCS bucket and returns public URLs.
/// </summary>
public class GcsFileUploadService : IFileUploadService, ITransientDependency
{
    private readonly ILogger<GcsFileUploadService> _logger;
    private readonly string _bucketName;
    private readonly StorageClient? _storageClient;

    public GcsFileUploadService(
        ILogger<GcsFileUploadService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _bucketName = configuration["GoogleCloud:StorageBucket"] ?? "klc-ev-charging-uploads";

        try
        {
            _storageClient = StorageClient.Create();
            _logger.LogInformation("GCS StorageClient initialized for bucket: {Bucket}", _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GCS StorageClient not initialized — file uploads will use local fallback");
        }
    }

    public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string folder)
    {
        var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var objectName = $"{folder}/{safeFileName}";

        if (_storageClient == null)
        {
            return await FallbackLocalUploadAsync(stream, fileName, folder, safeFileName);
        }

        try
        {
            var contentType = GetContentType(fileName);
            var obj = await _storageClient.UploadObjectAsync(
                _bucketName,
                objectName,
                contentType,
                stream);

            var url = $"https://storage.googleapis.com/{_bucketName}/{objectName}";

            _logger.LogInformation(
                "[GCS] Uploaded: {FileName} -> {Url} ({Size} bytes)",
                fileName, url, obj.Size);

            return new FileUploadResult
            {
                Url = url,
                FileSize = (long)(obj.Size ?? 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GCS] Upload failed for {FileName}, falling back to local", fileName);
            return await FallbackLocalUploadAsync(stream, fileName, folder, safeFileName);
        }
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        if (_storageClient == null)
        {
            _logger.LogWarning("[GCS] StorageClient not available, cannot delete: {Url}", fileUrl);
            return;
        }

        // Extract object name from URL: https://storage.googleapis.com/{bucket}/{objectName}
        var prefix = $"https://storage.googleapis.com/{_bucketName}/";
        if (!fileUrl.StartsWith(prefix)) return;

        var objectName = fileUrl[prefix.Length..];

        try
        {
            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
            _logger.LogInformation("[GCS] Deleted: {ObjectName}", objectName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("[GCS] Object not found (already deleted): {ObjectName}", objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GCS] Failed to delete: {ObjectName}", objectName);
        }
    }

    private async Task<FileUploadResult> FallbackLocalUploadAsync(
        Stream stream, string fileName, string folder, string safeFileName)
    {
        var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var folderPath = Path.Combine(uploadRoot, folder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, safeFileName);
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        var url = $"/uploads/{folder}/{safeFileName}";

        _logger.LogInformation("[LocalFallback] Uploaded: {FileName} -> {Url}", fileName, url);

        return new FileUploadResult
        {
            Url = url,
            FileSize = fileStream.Length
        };
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
