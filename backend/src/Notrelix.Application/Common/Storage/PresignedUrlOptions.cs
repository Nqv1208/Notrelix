using Microsoft.Extensions.Options;

namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Lifetimes of the pre-signed URLs issued by the storage provider, bound from <c>Storage:Presign</c>. The defaults are the stage 1 values:
/// five minutes for a download and for an upload window. A URL never outlives these bounds.
/// </summary>
public sealed class PresignedUrlOptions
{
    public const string SectionName = "Storage:Presign";

    /// <summary>How long a pre-signed PUT stays valid. Default 300 seconds.</summary>
    public int UploadExpirySeconds { get; init; } = 300;

    /// <summary>How long a pre-signed GET stays valid. Default 300 seconds.</summary>
    public int DownloadExpirySeconds { get; init; } = 300;
}

public sealed class PresignedUrlOptionsValidator : IValidateOptions<PresignedUrlOptions>
{
    /// <summary>The longest pre-signed URL the provider may issue (S3 allows up to seven days; stage 1 caps at one hour).</summary>
    public const int MaxExpirySeconds = 3600;

    public ValidateOptionsResult Validate(string? name, PresignedUrlOptions options)
    {
        var failures = new List<string>();
        if (options.UploadExpirySeconds is < 1 or > MaxExpirySeconds)
            failures.Add($"{PresignedUrlOptions.SectionName}:UploadExpirySeconds must be between 1 and {MaxExpirySeconds}.");
        if (options.DownloadExpirySeconds is < 1 or > MaxExpirySeconds)
            failures.Add($"{PresignedUrlOptions.SectionName}:DownloadExpirySeconds must be between 1 and {MaxExpirySeconds}.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
