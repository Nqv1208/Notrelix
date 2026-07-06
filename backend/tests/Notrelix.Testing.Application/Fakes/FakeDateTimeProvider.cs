using Notrelix.Testing.Core;

namespace Notrelix.Testing.Application.Fakes;

public class FakeDateTimeProvider : IDateTimeProvider
{
    private readonly TestClock _clock;

    public FakeDateTimeProvider(TestClock clock)
    {
        _clock = clock;
    }

    public DateTimeOffset UtcNow => _clock.UtcNow;

    public static FakeDateTimeProvider WithFixedTime(DateTimeOffset utcNow)
    {
        return new FakeDateTimeProvider(new TestClock { UtcNow = utcNow });
    }
}
