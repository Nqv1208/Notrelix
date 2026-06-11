using FluentAssertions;
using Notrelix.Domain.Identity.Security;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class LoginAttemptTests
{
    [Fact]
    public void Record_ShouldUseSuppliedTimestamp()
    {
        var occurredAt = new DateTimeOffset(2026, 6, 11, 10, 30, 0, TimeSpan.Zero);

        var attempt = UserLoginAttempt.Record(Guid.NewGuid(), "test@example.com", true, occurredAt, ipAddress: "192.168.1.1");

        attempt.OccurredAt.Should().Be(occurredAt);
        attempt.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Record_ShouldRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;

        var attempt = UserLoginAttempt.Record(Guid.NewGuid(), "test@example.com", false, now, "Invalid password");

        attempt.DomainEvents.Should().ContainSingle(e => e is LoginAttemptRecordedEvent);
        var evt = (LoginAttemptRecordedEvent)attempt.DomainEvents.First(e => e is LoginAttemptRecordedEvent);
        evt.Succeeded.Should().BeFalse();
        evt.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Record_FailedAttempt_ShouldStoreReason()
    {
        var now = DateTimeOffset.UtcNow;

        var attempt = UserLoginAttempt.Record(null, "unknown@example.com", false, now, "User not found", "10.0.0.1", "curl/7.0");

        attempt.UserId.Should().BeNull();
        attempt.Email.Should().Be("unknown@example.com");
        attempt.FailureReason.Should().Be("User not found");
        attempt.IpAddress.Should().Be("10.0.0.1");
        attempt.UserAgent.Should().Be("curl/7.0");
        attempt.Succeeded.Should().BeFalse();
    }
}
