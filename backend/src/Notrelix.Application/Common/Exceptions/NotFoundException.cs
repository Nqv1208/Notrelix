namespace Notrelix.Application.Common.Exceptions;

// Exception khi không tìm thấy resource
public class NotFoundException : Exception
{
    public NotFoundException() : base("Resource not found") { }
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key) : base($"Entity \"{name}\" ({key}) was not found.") { }

    /// <summary>A stable code when the caller may know why (for example an item deleted in a board they can read); null keeps the generic not-found.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Extra members of the public problem; only set after authorization already passed.</summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; init; } = new Dictionary<string, object?>();
}
