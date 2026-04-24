namespace Notrelix.Application.Common.Interfaces;

/// <summary>
/// Testable DateTime abstraction — tránh gọi DateTime.UtcNow trực tiếp
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
