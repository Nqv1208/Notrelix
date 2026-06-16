using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Security;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class ScimDirectorySyncTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseEvent()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);

        sync.WorkspaceId.Should().Be(WorkspaceId);
        sync.ProviderName.Should().Be("Azure AD");
        sync.Status.Should().Be(ScimSyncStatus.Enabled);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => ScimDirectorySync.Create(Guid.Empty, "Provider", UserId, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyProviderName_ShouldThrow()
    {
        var act = () => ScimDirectorySync.Create(WorkspaceId, "", UserId, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Pause_ShouldTransitionToPaused()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);
        sync.ClearDomainEvents();

        sync.Pause(UserId, Now);

        sync.Status.Should().Be(ScimSyncStatus.Paused);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncPausedDomainEvent);
    }

    [Fact]
    public void Pause_AlreadyPaused_ShouldBeIdempotent()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);
        sync.Pause(UserId, Now);
        sync.ClearDomainEvents();

        sync.Pause(UserId, Now);

        sync.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Resume_ShouldTransitionToEnabled()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);
        sync.Pause(UserId, Now);
        sync.ClearDomainEvents();

        sync.Resume(UserId, Now);

        sync.Status.Should().Be(ScimSyncStatus.Enabled);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncResumedDomainEvent);
    }

    [Fact]
    public void RecordSync_ShouldUpdateCursorAndRaiseEvent()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);
        sync.ClearDomainEvents();
        var syncAt = Now.AddHours(1);

        sync.RecordSync("{\"next\":\"abc\"}", syncAt);

        sync.LastSyncAt.Should().Be(syncAt);
        sync.CursorJson.Should().Be("{\"next\":\"abc\"}");
        sync.DomainEvents.Should().ContainSingle(e => e is ScimSyncCompletedDomainEvent);
    }

    [Fact]
    public void RecordSync_WithNullCursor_ShouldUseEmptyObject()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);

        sync.RecordSync(null!, Now);

        sync.CursorJson.Should().Be("{}");
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);

        sync.SoftDelete(UserId, Now);

        sync.IsDeleted.Should().BeTrue();
        sync.DomainEvents.Should().Contain(e => e is ScimDirectorySyncSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_AfterSoftDelete_ShouldSucceed()
    {
        var sync = ScimDirectorySync.Create(WorkspaceId, "Azure AD", UserId, Now);
        sync.SoftDelete(UserId, Now);
        sync.ClearDomainEvents();

        sync.Restore(UserId, Now);

        sync.IsDeleted.Should().BeFalse();
        sync.DomainEvents.Should().Contain(e => e is ScimDirectorySyncRestoredDomainEvent);
    }
}
