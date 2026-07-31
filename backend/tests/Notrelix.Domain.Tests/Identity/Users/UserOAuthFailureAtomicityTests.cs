using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity.Users;

public class UserOAuthFailureAtomicityTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static User CreateUser()
    {
        return User.Create("test@example.com", "Test User", "hash", Now);
    }

    private static OAuthProfileSnapshot CreateSnapshot(OAuthProvider provider = OAuthProvider.Google)
    {
        return OAuthProfileSnapshot.Create(provider, 1, JsonValue.EmptyObject());
    }

    private static OAuthToken CreateToken()
    {
        return OAuthToken.Create(
            SecretRef.Create("access-token-hash"),
            SecretRef.Create("refresh-token-hash"),
            Now.AddHours(1));
    }

    [CoversMutation(typeof(User), nameof(User.LinkOAuthAccount), MutationScenario.Valid, typeof(OAuthProvider), typeof(string), typeof(OAuthProfileSnapshot), typeof(OAuthToken), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void LinkOAuthAccount_WhenProviderMismatch_ShouldNotMutateRoot()
    {
        var user = CreateUser();
        var versionBefore = user.Version;
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Microsoft, 1, JsonValue.EmptyObject());

        var act = () => user.LinkOAuthAccount(
            OAuthProvider.Google, "provider-id", snapshot, CreateToken(), ActorId, Now);

        act.Should().Throw<BusinessRuleException>();
        user.Version.Should().Be(versionBefore);
        user.OAuthAccounts.Should().BeEmpty();
    }

    [CoversMutation(typeof(User), nameof(User.LinkOAuthAccount), MutationScenario.NoOp, typeof(OAuthProvider), typeof(string), typeof(OAuthProfileSnapshot), typeof(OAuthToken), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void LinkOAuthAccount_WhenAlreadyLinked_ShouldNotMutateRoot()
    {
        var user = CreateUser();
        user.LinkOAuthAccount(OAuthProvider.Google, "id1", CreateSnapshot(), CreateToken(), ActorId, Now);
        var versionBefore = user.Version;

        var act = () => user.LinkOAuthAccount(
            OAuthProvider.Google, "id2", CreateSnapshot(), CreateToken(), ActorId, Now);

        act.Should().Throw<BusinessRuleException>();
        user.Version.Should().Be(versionBefore);
        user.OAuthAccounts.Should().HaveCount(1);
    }

    [CoversMutation(typeof(User), nameof(User.UpdateOAuthProfile), MutationScenario.Valid, typeof(OAuthProvider), typeof(OAuthProfileSnapshot), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateOAuthProfile_WhenProviderNotFound_ShouldNotMutateRoot()
    {
        var user = CreateUser();
        var versionBefore = user.Version;

        var act = () => user.UpdateOAuthProfile(
            OAuthProvider.Google, CreateSnapshot(), ActorId, Now);

        act.Should().Throw<BusinessRuleException>();
        user.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(User), nameof(User.UpdateOAuthProfile), MutationScenario.Valid, typeof(OAuthProvider), typeof(OAuthProfileSnapshot), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateOAuthProfile_WhenProviderMismatch_ShouldNotMutateRoot()
    {
        var user = CreateUser();
        user.LinkOAuthAccount(OAuthProvider.Google, "id", CreateSnapshot(), CreateToken(), ActorId, Now);
        var versionBefore = user.Version;
        var wrongSnapshot = OAuthProfileSnapshot.Create(OAuthProvider.Microsoft, 1, JsonValue.EmptyObject());

        var act = () => user.UpdateOAuthProfile(
            OAuthProvider.Google, wrongSnapshot, ActorId, Now);

        act.Should().Throw<BusinessRuleException>();
        user.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(User), nameof(User.RotateOAuthToken), MutationScenario.Valid, typeof(OAuthProvider), typeof(OAuthToken), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateOAuthToken_WhenProviderNotFound_ShouldNotMutateRoot()
    {
        var user = CreateUser();
        var versionBefore = user.Version;

        var act = () => user.RotateOAuthToken(
            OAuthProvider.Google, CreateToken(), ActorId, Now);

        act.Should().Throw<BusinessRuleException>();
        user.Version.Should().Be(versionBefore);
    }

    [CoversMutation(typeof(User), nameof(User.UnlinkOAuthAccount), MutationScenario.Valid, typeof(OAuthProvider), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UnlinkOAuthAccount_WhenNotLinked_ShouldNotMutateRoot()
    {
        var user = CreateUser();
        var versionBefore = user.Version;

        user.UnlinkOAuthAccount(OAuthProvider.Google, ActorId, Now);

        user.Version.Should().Be(versionBefore);
        user.OAuthAccounts.Should().BeEmpty();
    }
}
