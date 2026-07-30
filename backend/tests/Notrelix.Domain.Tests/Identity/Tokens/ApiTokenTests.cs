using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity.Tokens;

[CoversAggregate(typeof(ApiToken))]
public class ApiTokenTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CreatedBy = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var scopes = ApiTokenScopes.FromJson("[\"read\"]");

        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "My Token", "hash123", scopes, CreatedBy, Now);

        token.WorkspaceId.Should().Be(WorkspaceId);
        token.UserId.Should().Be(UserId);
        token.Name.Should().Be("My Token");
        token.TokenHash.Should().Be("hash123");
        token.Scopes.Should().Be(scopes);
        token.Status.Should().Be(ApiTokenStatus.Active);
        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenCreatedDomainEvent);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "  My Token  ", "hash", null, CreatedBy, Now);

        token.Name.Should().Be("My Token");
    }

    [CoversMutation(typeof(ApiToken), "RecordUse(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Create_WithoutUserId_ShouldSucceed()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, null, "Token", "hash", null, CreatedBy, Now);

        token.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => ApiToken.Create(Guid.NewGuid(), Guid.Empty, UserId, "Token", "hash", null, CreatedBy, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(ApiToken), "Revoke(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Revoke_ShouldTransitionToRevokedAndRaiseEvent()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        token.Revoke(UserId, Now);

        token.Status.Should().Be(ApiTokenStatus.Revoked);
        token.RevokedAt.Should().Be(Now);
        token.RevokedBy.Should().Be(UserId);
        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenRevokedDomainEvent);
    }

    [CoversMutation(typeof(ApiToken), "Revoke(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Revoke_AlreadyRevoked_ShouldBeIdempotent()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now);
        token.Revoke(UserId, Now);
        ((IHasDomainEvents)token).ClearDomainEvents();

        token.Revoke(UserId, Now);

        token.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(ApiToken), "RecordUse(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void RecordUse_ShouldUpdateLastUsedAt()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now);
        ((IHasDomainEvents)token).ClearDomainEvents();
        var useTime = Now.AddHours(1);

        token.RecordUse(useTime);

        token.LastUsedAt.Should().Be(useTime);
    }

    [CoversMutation(typeof(ApiToken), "RecordUse(System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void RecordUse_ShouldSetAuditAndUpdateVersion()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now);
        ((IHasDomainEvents)token).ClearDomainEvents();
        var versionBefore = token.Version;
        var useTime = Now.AddHours(1);

        token.RecordUse(useTime);

        token.UpdatedAt.Should().Be(useTime);
        token.Version.Should().Be(versionBefore + 1);
    }

    [CoversMutation(typeof(ApiToken), "RecordUse(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void RecordUse_ShouldRaiseEvent()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now);
        ((IHasDomainEvents)token).ClearDomainEvents();
        var useTime = Now.AddHours(1);

        token.RecordUse(useTime);

        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenRecordedUseDomainEvent);
        var evt = (ApiTokenRecordedUseDomainEvent)token.DomainEvents.Single(e => e is ApiTokenRecordedUseDomainEvent);
        evt.TokenId.Should().Be(token.Id);
        evt.OccurredAt.Should().Be(useTime);
    }

    [CoversMutation(typeof(ApiToken), "RecordUse(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void RecordUse_ExpiredToken_ShouldThrow()
    {
        var expiresAt = Now.AddHours(1);
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now, expiresAt);

        var act = () => token.RecordUse(expiresAt.AddHours(1));

        act.Should().Throw<BusinessRuleException>().WithMessage("*expired*");
    }

    [CoversMutation(typeof(ApiToken), "RecordUse(System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(ApiToken), "Revoke(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void RecordUse_RevokedToken_ShouldThrow()
    {
        var token = ApiToken.Create(Guid.NewGuid(), WorkspaceId, UserId, "Token", "hash", null, CreatedBy, Now);
        token.Revoke(UserId, Now);

        var act = () => token.RecordUse(Now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*inactive*");
    }

}
