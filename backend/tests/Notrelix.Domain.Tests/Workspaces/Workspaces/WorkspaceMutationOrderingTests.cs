using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces.Workspaces;

public class WorkspaceMutationOrderingTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Workspace CreateWorkspace() =>
        Workspace.Create(Guid.NewGuid(), ActorId, "Test Workspace", "test-ws", Now);

    [CoversMutation(typeof(Workspace), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void Rename_ShouldPrepareAuditBeforeMutation()
    {
        var workspace = CreateWorkspace();
        workspace.Rename("New Name", ActorId, Now.AddMinutes(1));
        workspace.UpdatedBy.Should().Be(ActorId);
        workspace.UpdatedAt.Should().Be(Now.AddMinutes(1));
    }

    [CoversMutation(typeof(Workspace), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Rename_NoOp_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        var before = workspace.Version;
        workspace.Rename("Test Workspace", ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Rename_Archived_ShouldThrow()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        var act = () => workspace.Rename("New Name", ActorId, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Workspace), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void Archive_ShouldPrepareAuditBeforeMutation()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        workspace.UpdatedBy.Should().Be(ActorId);
    }

    [CoversMutation(typeof(Workspace), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Archive_NoOp_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        var before = workspace.Version;
        workspace.Archive(ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "Unarchive(System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void Unarchive_ShouldPrepareAuditBeforeMutation()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        workspace.Unarchive(ActorId, Now.AddMinutes(1));
        workspace.UpdatedBy.Should().Be(ActorId);
    }

    [CoversMutation(typeof(Workspace), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Unarchive_NonArchived_ShouldThrow()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        var act = () => workspace.Unarchive(ActorId, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Workspace), "Unarchive(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Unarchive_Active_NoOp_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        var before = workspace.Version;
        workspace.Unarchive(ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "UpdateDescription(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void UpdateDescription_ShouldPrepareAuditBeforeMutation()
    {
        var workspace = CreateWorkspace();
        workspace.UpdateDescription("New desc", ActorId, Now);
        workspace.UpdatedBy.Should().Be(ActorId);
    }

    [CoversMutation(typeof(Workspace), "UpdateDescription(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UpdateDescription_NoOp_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        var before = workspace.Version;
        workspace.UpdateDescription(null, ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void UpdateDescription_Archived_ShouldThrow()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        var act = () => workspace.UpdateDescription("New desc", ActorId, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(Workspace), "UpdateSettings(Notrelix.Domain.Workspaces.Workspaces.WorkspaceSettings,System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void UpdateSettings_ShouldPrepareAuditBeforeMutation()
    {
        var workspace = CreateWorkspace();
        var newSettings = WorkspaceSettings.Create(allowPublicSharing: true);
        workspace.UpdateSettings(newSettings, ActorId, Now);
        workspace.UpdatedBy.Should().Be(ActorId);
    }

    [CoversMutation(typeof(Workspace), "UpdateSettings(Notrelix.Domain.Workspaces.Workspaces.WorkspaceSettings,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UpdateSettings_NoOp_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        var settings = workspace.Settings;
        var before = workspace.Version;
        workspace.UpdateSettings(settings, ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void UpdateSettings_Archived_ShouldThrow()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        var act = () => workspace.UpdateSettings(WorkspaceSettings.Create(), ActorId, Now);
        act.Should().Throw<BusinessRuleException>();
    }
}
