using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Collaboration.Attachments;

/// <summary>
/// Collaboration policy for uploaded attachment files. Bound from
/// <c>Collaboration:Attachments</c> and validated at startup.
/// </summary>
public sealed class AttachmentUploadOptions
{
    public const string SectionName = "Collaboration:Attachments";

    /// <summary>Hard per-file size cap, enforced while the body is streamed. Default 100 MB.</summary>
    public long MaxFileSizeBytes { get; init; } = 100L * 1024 * 1024;

    /// <summary>Maximum number of (non-deleted) attachments on one board item, links included. Default 50.</summary>
    public int MaxAttachmentsPerItem { get; init; } = 50;

    /// <summary>Maximum number of (non-deleted) attachments on one Documents page, links included. Default 500.</summary>
    public int MaxAttachmentsPerPage { get; init; } = 500;

    /// <summary>
    /// File name extensions (lower case, without the dot) that are refused regardless of the
    /// sniffed content: executables and script types. Executable content signatures are
    /// refused independently of the extension.
    /// </summary>
    public string[] BlockedExtensions { get; init; } =
    [
        "exe", "msi", "dll", "bat", "cmd", "com", "scr", "ps1", "vbs", "sh", "jar", "apk", "js", "mjs", "html", "htm",
    ];
}

public sealed class AttachmentUploadOptionsValidator : IValidateOptions<AttachmentUploadOptions>
{
    public const long MinFileSizeBytes = 1024;
    public const long MaxConfigurableFileSizeBytes = 1L * 1024 * 1024 * 1024;
    public const int MaxConfigurableAttachments = 1000;

    public ValidateOptionsResult Validate(string? name, AttachmentUploadOptions options)
    {
        var failures = new List<string>();

        if (options.MaxFileSizeBytes is < MinFileSizeBytes or > MaxConfigurableFileSizeBytes)
        {
            failures.Add(
                $"{AttachmentUploadOptions.SectionName}:MaxFileSizeBytes must be between {MinFileSizeBytes} and {MaxConfigurableFileSizeBytes}.");
        }

        if (options.MaxAttachmentsPerItem is < 1 or > MaxConfigurableAttachments)
        {
            failures.Add(
                $"{AttachmentUploadOptions.SectionName}:MaxAttachmentsPerItem must be between 1 and {MaxConfigurableAttachments}.");
        }

        if (options.MaxAttachmentsPerPage is < 1 or > MaxConfigurableAttachments)
        {
            failures.Add(
                $"{AttachmentUploadOptions.SectionName}:MaxAttachmentsPerPage must be between 1 and {MaxConfigurableAttachments}.");
        }

        if (options.BlockedExtensions is null
            || options.BlockedExtensions.Any(e => string.IsNullOrWhiteSpace(e) || e.StartsWith('.') || e != e.ToLowerInvariant()))
        {
            failures.Add(
                $"{AttachmentUploadOptions.SectionName}:BlockedExtensions entries must be non-empty, lower case and without a leading dot.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
