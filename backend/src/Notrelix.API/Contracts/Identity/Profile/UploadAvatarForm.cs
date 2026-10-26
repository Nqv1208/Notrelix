namespace Notrelix.API.Contracts.Identity.Profile;

/// <summary>
/// OpenAPI description of the multipart/form-data body of the profile photo upload. The endpoint
/// reads the body as a stream and never binds this type. The part's own Content-Type and file name
/// are ignored: the accepted format (PNG, JPEG or WebP) is derived from the bytes.
/// </summary>
public sealed class UploadAvatarForm
{
    /// <summary>The image file part (PNG, JPEG or WebP, at most 2,000,000 bytes).</summary>
    public required IFormFile File { get; init; }
}
