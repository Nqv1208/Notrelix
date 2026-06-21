using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Mfa.Events;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Profiles.Events;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Security.Events;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Sessions.Events;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Users.Events;
using Xunit;

namespace Notrelix.Domain.Tests;

public class Phase4AuditTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    #region User SoftDelete / Restore

    [Fact]
    public void UserSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        var version = user.Version;

        user.SoftDelete(_actorId, _now);

        user.IsDeleted.Should().BeTrue();
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserSoftDeletedDomainEvent);
        var evt = (UserSoftDeletedDomainEvent)user.DomainEvents.Single(e => e is UserSoftDeletedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.DeletedBy.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }

    [Fact]
    public void UserRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.SoftDelete(_actorId, _now);
        user.ClearDomainEvents();
        var version = user.Version;

        user.Restore(_actorId, _now);

        user.IsDeleted.Should().BeFalse();
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserRestoredDomainEvent);
        var evt = (UserRestoredDomainEvent)user.DomainEvents.Single(e => e is UserRestoredDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.RestoredBy.Should().Be(_actorId);
    }

    [Fact]
    public void UserSoftDelete_ShouldNotIncrementOrRaiseEvent_WhenAlreadyDeleted()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.SoftDelete(_actorId, _now);
        user.ClearDomainEvents();
        var version = user.Version;

        user.SoftDelete(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().NotContain(e => e is UserSoftDeletedDomainEvent);
    }

    [Fact]
    public void UserRestore_ShouldNotIncrementOrRaiseEvent_WhenNotDeleted()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.ClearDomainEvents();
        var version = user.Version;

        user.Restore(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().NotContain(e => e is UserRestoredDomainEvent);
    }

    #endregion

    #region UserSession SoftDelete / Restore / UpdateRefreshToken

    [Fact]
    public void UserSessionUpdateRefreshToken_ShouldIncrementVersion_AndRaiseEvent()
    {
        var tokenHash = RefreshTokenHash.Create("refresh-token");
        var session = UserSession.Create(_actorId, tokenHash, _now.AddDays(30), _now);
        session.ClearDomainEvents();
        var version = session.Version;

        var newHash = RefreshTokenHash.Create("new-refresh-token");
        session.UpdateRefreshToken(newHash, _now);

        session.Version.Should().Be(version + 1);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRefreshTokenRotatedDomainEvent);
        var evt = (UserSessionRefreshTokenRotatedDomainEvent)session.DomainEvents.Single(e => e is UserSessionRefreshTokenRotatedDomainEvent);
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
    }

    [Fact]
    public void UserSessionSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var tokenHash = RefreshTokenHash.Create("refresh-token");
        var session = UserSession.Create(_actorId, tokenHash, _now.AddDays(30), _now);
        var version = session.Version;

        session.SoftDelete(_actorId, _now);

        session.IsDeleted.Should().BeTrue();
        session.Version.Should().Be(version + 1);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionSoftDeletedDomainEvent);
    }

    [Fact]
    public void UserSessionRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var tokenHash = RefreshTokenHash.Create("refresh-token");
        var session = UserSession.Create(_actorId, tokenHash, _now.AddDays(30), _now);
        session.SoftDelete(_actorId, _now);
        session.ClearDomainEvents();
        var version = session.Version;

        session.Restore(_actorId, _now);

        session.IsDeleted.Should().BeFalse();
        session.Version.Should().Be(version + 1);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRestoredDomainEvent);
    }

    #endregion

    #region ApiToken SoftDelete / Restore

    [Fact]
    public void ApiTokenSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var token = ApiToken.Create(workspaceId, _actorId, "My Token", "hash", null, _actorId, _now);
        var version = token.Version;

        token.SoftDelete(_actorId, _now);

        token.IsDeleted.Should().BeTrue();
        token.Version.Should().Be(version + 1);
        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenSoftDeletedDomainEvent);
        var evt = (ApiTokenSoftDeletedDomainEvent)token.DomainEvents.Single(e => e is ApiTokenSoftDeletedDomainEvent);
        evt.TokenId.Should().Be(token.Id);
    }

    [Fact]
    public void ApiTokenRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var token = ApiToken.Create(workspaceId, _actorId, "My Token", "hash", null, _actorId, _now);
        token.SoftDelete(_actorId, _now);
        token.ClearDomainEvents();
        var version = token.Version;

        token.Restore(_actorId, _now);

        token.IsDeleted.Should().BeFalse();
        token.Version.Should().Be(version + 1);
        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenRestoredDomainEvent);
    }

    #endregion

    #region UserMfaMethod SoftDelete / Restore

    [Fact]
    public void UserMfaMethodSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var secretRef = SecretRef.Create("secret-123");
        var method = UserMfaMethod.Create(_actorId, MfaMethodType.AuthenticatorApp, _now, secretRef);
        var version = method.Version;

        method.SoftDelete(_actorId, _now);

        method.IsDeleted.Should().BeTrue();
        method.Version.Should().Be(version + 1);
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodSoftDeletedDomainEvent);
    }

    [Fact]
    public void UserMfaMethodRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var secretRef = SecretRef.Create("secret-123");
        var method = UserMfaMethod.Create(_actorId, MfaMethodType.AuthenticatorApp, _now, secretRef);
        method.SoftDelete(_actorId, _now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.Restore(_actorId, _now);

        method.IsDeleted.Should().BeFalse();
        method.Version.Should().Be(version + 1);
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodRestoredDomainEvent);
    }

    #endregion

    #region UserSecuritySettings Create / SoftDelete / Restore

    [Fact]
    public void UserSecuritySettingsCreate_ShouldRaiseCreatedEvent()
    {
        var settings = UserSecuritySettings.Create(_actorId, _now);

        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsCreatedDomainEvent);
        var evt = (UserSecuritySettingsCreatedDomainEvent)settings.DomainEvents.Single(e => e is UserSecuritySettingsCreatedDomainEvent);
        evt.UserSecuritySettingsId.Should().Be(settings.Id);
        evt.UserId.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }

    [Fact]
    public void UserSecuritySettingsSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var settings = UserSecuritySettings.Create(_actorId, _now);
        settings.ClearDomainEvents();
        var version = settings.Version;

        settings.SoftDelete(_actorId, _now);

        settings.IsDeleted.Should().BeTrue();
        settings.Version.Should().Be(version + 1);
        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsSoftDeletedDomainEvent);
    }

    [Fact]
    public void UserSecuritySettingsRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var settings = UserSecuritySettings.Create(_actorId, _now);
        settings.SoftDelete(_actorId, _now);
        settings.ClearDomainEvents();
        var version = settings.Version;

        settings.Restore(_actorId, _now);

        settings.IsDeleted.Should().BeFalse();
        settings.Version.Should().Be(version + 1);
        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsRestoredDomainEvent);
    }

    #endregion

    #region UserProfile Create

    [Fact]
    public void UserProfileCreate_ShouldRaiseCreatedEvent()
    {
        var profile = UserProfile.Create(_actorId, _now);

        profile.DomainEvents.Should().ContainSingle(e => e is UserProfileCreatedDomainEvent);
        var evt = (UserProfileCreatedDomainEvent)profile.DomainEvents.Single(e => e is UserProfileCreatedDomainEvent);
        evt.UserProfileId.Should().Be(profile.Id);
        evt.UserId.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }

    #endregion

    #region SsoProvider Create / Disable / SoftDelete / Restore

    [Fact]
    public void SsoProviderCreate_ShouldRaiseCreatedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);

        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderCreatedDomainEvent);
        var evt = (SsoProviderCreatedDomainEvent)provider.DomainEvents.Single(e => e is SsoProviderCreatedDomainEvent);
        evt.ProviderId.Should().Be(provider.Id);
        evt.Name.Should().Be("My IdP");
    }

    [Fact]
    public void SsoProviderDisable_ShouldRaiseDisabledEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);
        provider.ClearDomainEvents();
        var version = provider.Version;

        provider.Disable(_actorId, _now);

        provider.Version.Should().Be(version + 1);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderDisabledDomainEvent);
    }

    [Fact]
    public void SsoProviderSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);
        provider.ClearDomainEvents();
        var version = provider.Version;

        provider.SoftDelete(_actorId, _now);

        provider.IsDeleted.Should().BeTrue();
        provider.Version.Should().Be(version + 1);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderSoftDeletedDomainEvent);
    }

    [Fact]
    public void SsoProviderRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);
        provider.SoftDelete(_actorId, _now);
        provider.ClearDomainEvents();
        var version = provider.Version;

        provider.Restore(_actorId, _now);

        provider.IsDeleted.Should().BeFalse();
        provider.Version.Should().Be(version + 1);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderRestoredDomainEvent);
    }

    #endregion

    #region ScimDirectorySync Create / Pause / Resume / SoftDelete / Restore

    [Fact]
    public void ScimDirectorySyncCreate_ShouldRaiseCreatedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);

        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncCreatedDomainEvent);
        var evt = (ScimDirectorySyncCreatedDomainEvent)sync.DomainEvents.Single(e => e is ScimDirectorySyncCreatedDomainEvent);
        evt.SyncId.Should().Be(sync.Id);
        evt.ProviderName.Should().Be("Azure AD");
    }

    [Fact]
    public void ScimDirectorySyncPause_ShouldRaisePausedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.Pause(_actorId, _now);

        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncPausedDomainEvent);
    }

    [Fact]
    public void ScimDirectorySyncResume_ShouldRaiseResumedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.Pause(_actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.Resume(_actorId, _now);

        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncResumedDomainEvent);
    }

    [Fact]
    public void ScimDirectorySyncSoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.SoftDelete(_actorId, _now);

        sync.IsDeleted.Should().BeTrue();
        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncSoftDeletedDomainEvent);
    }

    [Fact]
    public void ScimDirectorySyncRestore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.SoftDelete(_actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.Restore(_actorId, _now);

        sync.IsDeleted.Should().BeFalse();
        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncRestoredDomainEvent);
    }

    #endregion
}
