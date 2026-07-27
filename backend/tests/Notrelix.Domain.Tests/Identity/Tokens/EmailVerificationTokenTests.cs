using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity.Tokens;

public class EmailVerificationTokenTests
{
    private static readonly TokenHash ValidHash = TokenHash.Create("test-verification-hash");

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var token = EmailVerificationToken.Create(userId, ValidHash, now.AddHours(24), now);

        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be(ValidHash);
        token.Status.Should().Be(UserTokenStatus.Active);
        token.ExpiresAt.Should().Be(now.AddHours(24));
        token.CreatedAt.Should().Be(now);
        token.UsedAt.Should().BeNull();
        token.ExpiredAt.Should().BeNull();

        token.DomainEvents.Should().ContainSingle(e => e is EmailVerificationTokenCreatedDomainEvent);
        var evt = (EmailVerificationTokenCreatedDomainEvent)token.DomainEvents.Single(e => e is EmailVerificationTokenCreatedDomainEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(userId);
        evt.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithInvalidExpiration_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*after creation time*");
    }

    [Fact]
    public void MarkUsed_ShouldChangeStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(24), now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        var useTime = now.AddHours(1);
        token.MarkUsed(useTime);

        token.Status.Should().Be(UserTokenStatus.Used);
        token.UsedAt.Should().Be(useTime);

        token.DomainEvents.Should().ContainSingle(e => e is EmailVerificationTokenUsedDomainEvent);
        var evt = (EmailVerificationTokenUsedDomainEvent)token.DomainEvents.Single(e => e is EmailVerificationTokenUsedDomainEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(token.UserId);
        evt.UsedAt.Should().Be(useTime);
    }

    [Fact]
    public void MarkUsed_AlreadyUsed_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(24), now);
        token.MarkUsed(now.AddHours(1));

        var act = () => token.MarkUsed(now.AddHours(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*already been used*");
    }

    [Fact]
    public void MarkUsed_AfterExpiresAt_ShouldThrowWithoutMutating()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(1), now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        var useTime = now.AddHours(2);
        var act = () => token.MarkUsed(useTime);

        act.Should().Throw<BusinessRuleException>().WithMessage("*expired token*");
        token.Status.Should().Be(UserTokenStatus.Active);
        token.ExpiredAt.Should().BeNull();
        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Expire_ShouldSetStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(24), now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        var expireTime = now.AddHours(1);
        token.Expire(expireTime);

        token.Status.Should().Be(UserTokenStatus.Expired);
        token.ExpiredAt.Should().Be(expireTime);

        token.DomainEvents.Should().ContainSingle(e => e is EmailVerificationTokenExpiredDomainEvent);
        var evt = (EmailVerificationTokenExpiredDomainEvent)token.DomainEvents.Single(e => e is EmailVerificationTokenExpiredDomainEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.UserId.Should().Be(token.UserId);
        evt.ExpiredAt.Should().Be(expireTime);
    }

    [Fact]
    public void Expire_AlreadyExpired_ShouldBeIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(24), now);
        token.Expire(now.AddHours(1));
        ((IHasDomainEvents)token).ClearDomainEvents();

        token.Expire(now.AddHours(2));

        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Expire_OnUsedToken_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(Guid.NewGuid(), ValidHash, now.AddHours(24), now);
        token.MarkUsed(now.AddHours(1));

        var act = () => token.Expire(now.AddHours(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*used token*");
    }

    [Fact]
    public void Revoke_ShouldMakeTokenUnavailable()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            ValidHash,
            1,
            "person@example.com",
            now.AddHours(1),
            now);

        token.TryRevoke(now.AddMinutes(1), "resend").Should().BeTrue();
        token.Status.Should().Be(UserTokenStatus.Revoked);
        token.RevokedAt.Should().Be(now.AddMinutes(1));
        token.RevocationReason.Should().Be("resend");
    }

    [Fact]
    public void Create_ShouldSnapshotNormalizedEmailAndHashVersion()
    {
        var now = DateTimeOffset.UtcNow;
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            ValidHash,
            3,
            " PERSON@EXAMPLE.COM ",
            now.AddHours(1),
            now);

        token.HashVersion.Should().Be(3);
        token.NormalizedEmailSnapshot.Should().Be("person@example.com");
    }
}
