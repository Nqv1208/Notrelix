using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Data;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
