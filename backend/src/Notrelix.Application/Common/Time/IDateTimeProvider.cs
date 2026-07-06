namespace Notrelix.Application.Common.Time;

/// <summary>
/// Testable DateTime abstraction — tránh gọi DateTime.UtcNow trực tiếp
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
