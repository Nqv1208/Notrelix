using FluentAssertions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity.Users;

public class UserActorSemanticsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static User CreateUser() => User.Create("test@example.com", "Test User", "hash", Now);

    [CoversMutation(typeof(User), nameof(User.UpdateProfile), MutationScenario.Valid, typeof(string), typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateProfile_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.UpdateProfile("New Name", null, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.UpdateEmail), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateEmail_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.UpdateEmail("new@example.com", actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.UpdatePassword), MutationScenario.Valid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdatePassword_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.UpdatePassword("new-hash", actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.RecordLogin), MutationScenario.Valid, typeof(DateTimeOffset))]
    [Fact]
    public void RecordLogin_ShouldSetUpdatedByToSelf()
    {
        var user = CreateUser();
        user.RecordLogin(Now.AddMinutes(1));
        user.UpdatedBy.Should().Be(user.Id);
    }

    [CoversMutation(typeof(User), nameof(User.Activate), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Activate_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        user.Deactivate(Actor, Now);
        var actor = Guid.NewGuid();
        user.Activate(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.Deactivate), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Deactivate_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.Deactivate(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.Suspend), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.Suspend(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.ConfirmEmail), MutationScenario.Valid, typeof(Guid?), typeof(DateTimeOffset))]
    [Fact]
    public void ConfirmEmail_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.ConfirmEmail(actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.ConfirmEmail), MutationScenario.Valid, typeof(Guid?), typeof(DateTimeOffset))]
    [Fact]
    public void ConfirmEmail_SystemActor_ShouldAllowNull()
    {
        var user = CreateUser();
        user.ConfirmEmail(null, Now);
        user.UpdatedBy.Should().BeNull();
    }

    [CoversMutation(typeof(User), nameof(User.LinkOAuthAccount), MutationScenario.Valid, typeof(OAuthProvider), typeof(string), typeof(OAuthProfileSnapshot), typeof(OAuthToken), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void LinkOAuthAccount_ShouldSetUpdatedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        user.LinkOAuthAccount(OAuthProvider.Google, "id", snapshot, null, actor, Now);
        user.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.UpdateOAuthProfile), MutationScenario.Valid, typeof(OAuthProvider), typeof(OAuthProfileSnapshot), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(User), nameof(User.RotateOAuthToken), MutationScenario.Valid, typeof(OAuthProvider), typeof(OAuthToken), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(User), nameof(User.UnlinkOAuthAccount), MutationScenario.Valid, typeof(OAuthProvider), typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(User), nameof(User.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldSetDeletedByToActor()
    {
        var user = CreateUser();
        var actor = Guid.NewGuid();
        user.Delete(actor, Now);
        user.DeletedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(User), nameof(User.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldSetRestoredByToActor()
    {
        var user = CreateUser();
        user.Delete(Guid.NewGuid(), Now);
        var actor = Guid.NewGuid();
        user.Restore(actor, Now);
    }

    private static Guid Actor => Guid.NewGuid();
}
