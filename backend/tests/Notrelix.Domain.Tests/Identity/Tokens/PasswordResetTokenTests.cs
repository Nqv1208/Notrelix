using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity.Tokens;

public class PasswordResetTokenTests
{
    private static readonly TokenHash ValidHash = TokenHash.Create("test-reset-hash");

    [CoversMutation(typeof(PasswordResetToken), "Revoke(System.DateTimeOffset,System.String)", MutationScenario.Valid)]
    [CoversMutation(typeof(PasswordResetToken), "TryRevoke(System.DateTimeOffset,System.String,Notrelix.Domain.Common.DomainEvent)", MutationScenario.Valid)]
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

        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenCreatedDomainEvent);
        var evt = (PasswordResetTokenCreatedDomainEvent)token.DomainEvents.Single(e => e is PasswordResetTokenCreatedDomainEvent);
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

    [CoversMutation(typeof(PasswordResetToken), "MarkUsed(System.DateTimeOffset,Notrelix.Domain.Common.DomainEvent)", MutationScenario.Event)]
    [CoversMutation(typeof(PasswordResetToken), "MarkUsed(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void MarkUsed_ShouldChangeStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        var useTime = now.AddMinutes(15);
        token.MarkUsed(useTime);

        token.Status.Should().Be(UserTokenStatus.Used);
        token.UsedAt.Should().Be(useTime);

        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenUsedDomainEvent);
        var evt = (PasswordResetTokenUsedDomainEvent)token.DomainEvents.Single(e => e is PasswordResetTokenUsedDomainEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(token.UserId);
        evt.UsedAt.Should().Be(useTime);
    }

    [CoversMutation(typeof(PasswordResetToken), "MarkUsed(System.DateTimeOffset,Notrelix.Domain.Common.DomainEvent)", MutationScenario.NoOp)]
    [CoversMutation(typeof(PasswordResetToken), "MarkUsed(System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void MarkUsed_AlreadyUsed_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.MarkUsed(now.AddMinutes(15));

        var act = () => token.MarkUsed(now.AddMinutes(30));

        act.Should().Throw<BusinessRuleException>().WithMessage("*already been used*");
    }

    [CoversMutation(typeof(PasswordResetToken), "TryExpire(System.DateTimeOffset,Notrelix.Domain.Common.DomainEvent)", MutationScenario.Invalid)]
    [CoversMutation(typeof(PasswordResetToken), "Expire(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void MarkUsed_AfterExpiresAt_ShouldThrowWithoutMutating()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        var useTime = now.AddHours(2);
        var act = () => token.MarkUsed(useTime);

        act.Should().Throw<BusinessRuleException>().WithMessage("*expired token*");
        token.Status.Should().Be(UserTokenStatus.Active);
        token.ExpiredAt.Should().BeNull();
        token.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(PasswordResetToken), "TryExpire(System.DateTimeOffset,Notrelix.Domain.Common.DomainEvent)", MutationScenario.Event)]
    [CoversMutation(typeof(PasswordResetToken), "Expire(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Expire_ShouldSetStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        var expireTime = now.AddMinutes(15);
        token.Expire(expireTime);

        token.Status.Should().Be(UserTokenStatus.Expired);
        token.ExpiredAt.Should().Be(expireTime);

        token.DomainEvents.Should().ContainSingle(e => e is PasswordResetTokenExpiredDomainEvent);
        var evt = (PasswordResetTokenExpiredDomainEvent)token.DomainEvents.Single(e => e is PasswordResetTokenExpiredDomainEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(token.UserId);
        evt.ExpiredAt.Should().Be(expireTime);
    }

    [CoversMutation(typeof(PasswordResetToken), "TryExpire(System.DateTimeOffset,Notrelix.Domain.Common.DomainEvent)", MutationScenario.NoOp)]
    [CoversMutation(typeof(PasswordResetToken), "Expire(System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Expire_AlreadyExpired_ShouldBeIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        token.Expire(now.AddMinutes(15));
        ((IHasDomainEvents)token).ClearDomainEvents();

        token.Expire(now.AddMinutes(30));

        token.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(PasswordResetToken), "TryExpire(System.DateTimeOffset,Notrelix.Domain.Common.DomainEvent)", MutationScenario.Invalid)]
    [CoversMutation(typeof(PasswordResetToken), "Expire(System.DateTimeOffset)", MutationScenario.Invalid)]
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
