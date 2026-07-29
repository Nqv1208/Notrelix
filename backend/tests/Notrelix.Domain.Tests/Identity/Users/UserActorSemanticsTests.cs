using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity.Users;

public class UserActorSemanticsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static User CreateUser() => User.Create("test@example.com", "Test User", "hash", Now);

    [CoversMutation(typeof(User), "UpdateProfile(System.String,System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UpdateProfile_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.UpdateProfile("New Name", null, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "UpdateEmail(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UpdateEmail_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.UpdateEmail("new@example.com", actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "UpdatePassword(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UpdatePassword_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.UpdatePassword("new-hash", actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "RecordLogin(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void RecordLogin_ShouldSetUpdatedByToSelf()
    {
        var user = CreateUser();
        user.RecordLogin(Now.AddMinutes(1));
        user.UpdatedBy.Should().Be(user.Id);
    }

    [CoversMutation(typeof(User), "Activate(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Valid)]
    [Fact]
    public void Activate_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        user.Deactivate(Actor, Now);
        var actor = Guid.NewGuid();
        user.Activate(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "Deactivate(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Valid)]
    [Fact]
    public void Deactivate_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.Deactivate(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "Suspend(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Valid)]
    [Fact]
    public void Suspend_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.Suspend(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "ConfirmEmail(System.Guid?,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ConfirmEmail_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.ConfirmEmail(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "ConfirmEmail(System.Guid?,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ConfirmEmail_SystemActor_ShouldAllowNull()
    {
        var user = CreateUser();
        user.ConfirmEmail(null, Now);
        user.UpdatedBy.Should().BeNull();
    }

    [CoversMutation(typeof(User), "LinkOAuthAccount(Notrelix.Domain.Identity.OAuth.OAuthProvider,System.String,Notrelix.Domain.Identity.OAuth.OAuthProfileSnapshot,Notrelix.Domain.Identity.OAuth.OAuthToken,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void LinkOAuthAccount_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, null, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "UpdateOAuthProfile(Notrelix.Domain.Identity.OAuth.OAuthProvider,Notrelix.Domain.Identity.OAuth.OAuthProfileSnapshot,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UpdateOAuthProfile_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, null, Guid.NewGuid(), Now);
        var newSnapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 2, JsonValue.EmptyObject());
        user.UpdateOAuthProfile(OAuthProvider.Google, newSnapshot, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "RotateOAuthToken(Notrelix.Domain.Identity.OAuth.OAuthProvider,Notrelix.Domain.Identity.OAuth.OAuthToken,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void RotateOAuthToken_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        var token = OAuthToken.Create(SecretRef.Create("token"));
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, token, Guid.NewGuid(), Now);
        var newToken = OAuthToken.Create(SecretRef.Create("new-token"));
        user.RotateOAuthToken(OAuthProvider.Google, newToken, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "UnlinkOAuthAccount(Notrelix.Domain.Identity.OAuth.OAuthProvider,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UnlinkOAuthAccount_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, null, Guid.NewGuid(), Now);
        user.UnlinkOAuthAccount(OAuthProvider.Google, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldSetDeletedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.SoftDelete(actor, Now);
        user.DeletedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSetRestoredByToActor()
    {
        var user = CreateUser();
        user.SoftDelete(Guid.NewGuid(), Now);
        var actor = Guid.NewGuid();
        user.Restore(actor, Now);
        user.RestoredBy.Should().Be(actor);
    }

    private static Guid Actor => Guid.NewGuid();
}
