namespace Notrelix.Application.Common.Abstractions;

/// <summary>
/// Interface cho file storage service (S3, R2, hoặc local)
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload file và trả về URL public
    /// </summary>
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa file theo URL hoặc key
    /// </summary>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo presigned URL cho upload trực tiếp từ client
    /// </summary>
    Task<string> GetPresignedUploadUrlAsync(string fileName, string contentType, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}
