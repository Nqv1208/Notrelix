using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserLoginAttemptTests
{
    [Fact]
    public void Record_Success_ShouldSetPropertiesAndRaiseEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var attempt = UserLoginAttempt.Record(
            userId: userId,
            attemptedEmail: "test@example.com",
            succeeded: true,
            occurredAt: now,
            ipAddress: "127.0.0.1",
            userAgent: "Mozilla"
        );

        attempt.UserId.Should().Be(userId);
        attempt.AttemptedEmail.Should().Be("test@example.com");
        attempt.Succeeded.Should().BeTrue();
        attempt.FailureReason.Should().BeNull();
        attempt.IpAddress.Should().Be("127.0.0.1");
        attempt.UserAgent.Should().Be("Mozilla");
        attempt.OccurredAt.Should().Be(now);

        attempt.DomainEvents.Should().ContainSingle(e => e is LoginAttemptRecordedDomainEvent);
        var evt = (LoginAttemptRecordedDomainEvent)attempt.DomainEvents.Single(e => e is LoginAttemptRecordedDomainEvent);
        evt.LoginAttemptId.Should().Be(attempt.Id);
        evt.UserId.Should().Be(userId);
        evt.AttemptedEmail.Should().Be("test@example.com");
        evt.Succeeded.Should().BeTrue();
        evt.FailureReason.Should().BeNull();
        evt.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void Record_Failure_ShouldSetFailureReasonAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;

        var attempt = UserLoginAttempt.Record(
            userId: null,
            attemptedEmail: "test@example.com",
            succeeded: false,
            occurredAt: now,
            failureReason: LoginFailureReason.InvalidCredentials,
            ipAddress: "127.0.0.1"
        );

        attempt.UserId.Should().BeNull();
        attempt.AttemptedEmail.Should().Be("test@example.com");
        attempt.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(LoginFailureReason.InvalidCredentials);
        attempt.OccurredAt.Should().Be(now);

        attempt.DomainEvents.Should().ContainSingle(e => e is LoginAttemptRecordedDomainEvent);
        var evt = (LoginAttemptRecordedDomainEvent)attempt.DomainEvents.Single(e => e is LoginAttemptRecordedDomainEvent);
        evt.LoginAttemptId.Should().Be(attempt.Id);
        evt.UserId.Should().BeNull();
        evt.AttemptedEmail.Should().Be("test@example.com");
        evt.Succeeded.Should().BeFalse();
        evt.FailureReason.Should().Be(LoginFailureReason.InvalidCredentials);
        evt.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void Record_WithoutUserAndEmail_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => UserLoginAttempt.Record(
            userId: null,
            attemptedEmail: null,
            succeeded: true,
            occurredAt: now
        );

        act.Should().Throw<BusinessRuleException>().WithMessage("*either user id or attempted email*");
    }

    [Fact]
    public void Record_SuccessWithFailureReason_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => UserLoginAttempt.Record(
            userId: Guid.NewGuid(),
            attemptedEmail: "test@example.com",
            succeeded: true,
            occurredAt: now,
            failureReason: LoginFailureReason.InvalidCredentials
        );

        act.Should().Throw<BusinessRuleException>().WithMessage("*cannot have failure reason*");
    }

    [Fact]
    public void Record_FailureWithoutReason_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => UserLoginAttempt.Record(
            userId: Guid.NewGuid(),
            attemptedEmail: "test@example.com",
            succeeded: false,
            occurredAt: now,
            failureReason: null
        );

        act.Should().Throw<BusinessRuleException>().WithMessage("*must have failure reason*");
    }
}
