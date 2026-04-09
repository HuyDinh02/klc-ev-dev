namespace KLC.Configuration;

/// <summary>
/// File storage configuration. Supports multiple providers:
/// - "gcs" (Google Cloud Storage) — default for GCP deployments
/// - "s3" (S3-compatible) — for CMC Cloud, AWS, MinIO, etc.
/// - "local" (local filesystem) — for development
/// </summary>
public class FileStorageSettings
{
    public const string Section = "FileStorage";

    /// <summary>
    /// Storage provider: "gcs", "s3", or "local". Default: "gcs".
    /// </summary>
    public string Provider { get; set; } = "gcs";

    /// <summary>GCS bucket name (when Provider = "gcs")</summary>
    public string GcsBucket { get; set; } = "klc-ev-charging-uploads";

    /// <summary>S3 endpoint URL (when Provider = "s3")</summary>
    public string S3Endpoint { get; set; } = "";

    /// <summary>S3 bucket name (when Provider = "s3")</summary>
    public string S3Bucket { get; set; } = "klc-ev-charging-uploads";

    /// <summary>S3 access key (when Provider = "s3")</summary>
    public string S3AccessKey { get; set; } = "";

    /// <summary>S3 secret key (when Provider = "s3")</summary>
    public string S3SecretKey { get; set; } = "";

    /// <summary>S3 region (when Provider = "s3")</summary>
    public string S3Region { get; set; } = "ap-southeast-1";

    /// <summary>Local upload directory (when Provider = "local")</summary>
    public string LocalPath { get; set; } = "uploads";

    /// <summary>Base URL for generating public file URLs</summary>
    public string? PublicBaseUrl { get; set; }
}
