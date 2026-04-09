using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using KLC.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KLC.Files;

/// <summary>
/// S3-compatible file upload service.
/// Works with AWS S3, CMC Cloud S3, MinIO, and any S3-compatible storage.
/// </summary>
public class S3FileUploadService : IFileUploadService
{
    private readonly ILogger<S3FileUploadService> _logger;
    private readonly FileStorageSettings _settings;
    private readonly AmazonS3Client? _s3Client;

    public S3FileUploadService(
        ILogger<S3FileUploadService> logger,
        IOptions<FileStorageSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        try
        {
            var config = new AmazonS3Config
            {
                ServiceURL = _settings.S3Endpoint,
                ForcePathStyle = true // Required for non-AWS S3 providers (CMC, MinIO)
            };

            _s3Client = new AmazonS3Client(
                _settings.S3AccessKey,
                _settings.S3SecretKey,
                config);

            _logger.LogInformation("[S3] Client initialized: endpoint={Endpoint}, bucket={Bucket}",
                _settings.S3Endpoint, _settings.S3Bucket);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[S3] Client not initialized — file uploads will fail");
        }
    }

    public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string folder)
    {
        if (_s3Client == null)
            throw new InvalidOperationException("S3 client not initialized");

        var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var objectKey = $"{folder}/{safeFileName}";
        var contentType = GetContentType(fileName);

        var request = new PutObjectRequest
        {
            BucketName = _settings.S3Bucket,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        var response = await _s3Client.PutObjectAsync(request);

        var url = !string.IsNullOrEmpty(_settings.PublicBaseUrl)
            ? $"{_settings.PublicBaseUrl.TrimEnd('/')}/{objectKey}"
            : $"{_settings.S3Endpoint.TrimEnd('/')}/{_settings.S3Bucket}/{objectKey}";

        _logger.LogInformation("[S3] Uploaded: {FileName} -> {Url}", fileName, url);

        return new FileUploadResult
        {
            Url = url,
            FileSize = stream.Length
        };
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl) || _s3Client == null) return;

        // Extract object key from URL
        var objectKey = ExtractObjectKey(fileUrl);
        if (string.IsNullOrEmpty(objectKey)) return;

        try
        {
            await _s3Client.DeleteObjectAsync(_settings.S3Bucket, objectKey);
            _logger.LogInformation("[S3] Deleted: {ObjectKey}", objectKey);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("[S3] Object not found (already deleted): {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[S3] Failed to delete: {ObjectKey}", objectKey);
        }
    }

    private string? ExtractObjectKey(string fileUrl)
    {
        // Try PublicBaseUrl prefix
        if (!string.IsNullOrEmpty(_settings.PublicBaseUrl))
        {
            var prefix = _settings.PublicBaseUrl.TrimEnd('/') + "/";
            if (fileUrl.StartsWith(prefix)) return fileUrl[prefix.Length..];
        }

        // Try S3 endpoint prefix
        var s3Prefix = $"{_settings.S3Endpoint.TrimEnd('/')}/{_settings.S3Bucket}/";
        if (fileUrl.StartsWith(s3Prefix)) return fileUrl[s3Prefix.Length..];

        return null;
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
