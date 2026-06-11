using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Credentials;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class CredentialTokenTests
{
    private static readonly TokenHash ValidHash = TokenHash.Create("raw-token");

    [Fact]
    public void PasswordResetToken_Create_ShouldRaiseRequestedEvent()
    {
        var now = DateTimeOffset.UtcNow;

        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);

        token.Status.Should().Be(CredentialTokenStatus.Active);
        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetRequestedEvent);
    }

    [Fact]
    public void PasswordResetToken_Consume_ShouldChangeStatusAndRaiseCompletedEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.ClearDomainEvents();

        token.Consume(now.AddMinutes(5));

        token.Status.Should().Be(CredentialTokenStatus.Consumed);
        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetCompletedEvent);
    }

    [Fact]
    public void PasswordResetToken_Consume_AlreadyConsumed_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.Consume(now.AddMinutes(5));

        var act = () => token.Consume(now.AddMinutes(10));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PasswordResetToken_Expire_ShouldChangeStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);

        token.Expire();

        token.Status.Should().Be(CredentialTokenStatus.Expired);
    }

    [Fact]
    public void EmailVerificationToken_Create_ShouldRaiseRequestedEvent()
    {
        var now = DateTimeOffset.UtcNow;

        var token = EmailVerificationToken.Create(Guid.NewGuid(), "test@example.com", ValidHash, now.AddHours(24), now);

        token.Email.Should().Be("test@example.com");
        token.Status.Should().Be(CredentialTokenStatus.Active);
        token.DomainEvents.Should().ContainSingle(e => e is EmailVerificationRequestedEvent);
    }

    [Fact]
    public void EmailVerificationToken_Consume_ShouldRaiseCompletedEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "test@example.com", ValidHash, now.AddHours(24), now);
        token.ClearDomainEvents();

        token.Consume(now.AddHours(1));

        token.Status.Should().Be(CredentialTokenStatus.Consumed);
        token.DomainEvents.Should().ContainSingle(e => e is EmailVerificationCompletedEvent);
    }

    [Fact]
    public void EmailVerificationToken_Consume_AlreadyConsumed_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "test@example.com", ValidHash, now.AddHours(24), now);
        token.Consume(now.AddHours(1));

        var act = () => token.Consume(now.AddHours(2));

        act.Should().Throw<DomainException>();
    }
}
