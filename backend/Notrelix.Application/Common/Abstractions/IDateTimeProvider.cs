namespace Notrelix.Application.Common.Abstractions;

/// <summary>
/// Testable DateTime abstraction — tránh gọi DateTime.UtcNow trực tiếp
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
