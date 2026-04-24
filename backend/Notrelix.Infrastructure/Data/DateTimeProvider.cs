using Notrelix.Application.Common.Interfaces;

namespace Notrelix.Infrastructure.Data;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
