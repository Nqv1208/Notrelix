using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Tokens.Events;
using Xunit;

namespace Notrelix.Domain.Tests.Identity.Tokens;

public class PasswordResetTokenTests
{
    private static readonly TokenHash ValidHash = TokenHash.Create("test-reset-hash");

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var token = PasswordResetToken.Create(userId, ValidHash, now.AddHours(1), now);

        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be(ValidHash);
        token.Status.Should().Be(UserTokenStatus.Active);
        token.ExpiresAt.Should().Be(now.AddHours(1));
        token.CreatedAt.Should().Be(now);
        token.UsedAt.Should().BeNull();
        token.ExpiredAt.Should().BeNull();

        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenCreatedEvent);
        var evt = (PasswordResetTokenCreatedEvent)token.DomainEvents.Single(e => e is PasswordResetTokenCreatedEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(userId);
        evt.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithInvalidExpiration_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*after creation time*");
    }

    [Fact]
    public void MarkUsed_ShouldChangeStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.ClearDomainEvents();

        var useTime = now.AddMinutes(15);
        token.MarkUsed(useTime);

        token.Status.Should().Be(UserTokenStatus.Used);
        token.UsedAt.Should().Be(useTime);

        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenUsedEvent);
        var evt = (PasswordResetTokenUsedEvent)token.DomainEvents.Single(e => e is PasswordResetTokenUsedEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(token.UserId);
        evt.UsedAt.Should().Be(useTime);
    }

    [Fact]
    public void MarkUsed_AlreadyUsed_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.MarkUsed(now.AddMinutes(15));

        var act = () => token.MarkUsed(now.AddMinutes(30));

        act.Should().Throw<BusinessRuleException>().WithMessage("*already been used*");
    }

    [Fact]
    public void MarkUsed_AfterExpiresAt_ShouldTransitionToExpiredAndThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.ClearDomainEvents();

        var useTime = now.AddHours(2);
        var act = () => token.MarkUsed(useTime);

        act.Should().Throw<BusinessRuleException>().WithMessage("*expired token*");
        token.Status.Should().Be(UserTokenStatus.Expired);
        token.ExpiredAt.Should().Be(useTime);
        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenExpiredEvent);
    }

    [Fact]
    public void Expire_ShouldSetStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.ClearDomainEvents();

        var expireTime = now.AddMinutes(15);
        token.Expire(expireTime);

        token.Status.Should().Be(UserTokenStatus.Expired);
        token.ExpiredAt.Should().Be(expireTime);

        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenExpiredEvent);
        var evt = (PasswordResetTokenExpiredEvent)token.DomainEvents.Single(e => e is PasswordResetTokenExpiredEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(token.UserId);
        evt.ExpiredAt.Should().Be(expireTime);
    }

    [Fact]
    public void Expire_AlreadyExpired_ShouldBeIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.Expire(now.AddMinutes(15));
        token.ClearDomainEvents();

        token.Expire(now.AddMinutes(30));

        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Expire_OnUsedToken_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.MarkUsed(now.AddMinutes(15));

        var act = () => token.Expire(now.AddMinutes(30));

        act.Should().Throw<BusinessRuleException>().WithMessage("*used token*");
    }
}
