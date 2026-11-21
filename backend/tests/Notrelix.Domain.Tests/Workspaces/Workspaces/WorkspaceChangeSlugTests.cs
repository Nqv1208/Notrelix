using FluentAssertions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceChangeSlugTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private Workspace NewWorkspace()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Acme", "acme", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        return workspace;
    }

    [Fact]
    public void ChangeSlug_ANewAddress_StoresItNormalised_BumpsTheVersionOnce_AndRaisesOneEvent()
    {
        var workspace = NewWorkspace();
        var version = workspace.Version;

        workspace.ChangeSlug("  Acme-Team ", _actorId, _now);

        workspace.Slug.Should().Be("acme-team");
        workspace.Version.Should().Be(version + 1);
        var evt = workspace.DomainEvents.OfType<WorkspaceSlugChangedDomainEvent>().Single();
        evt.OldSlug.Should().Be("acme");
        evt.NewSlug.Should().Be("acme-team");
        evt.AccountId.Should().Be(_accountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
        evt.UpdatedBy.Should().Be(_actorId);
        workspace.DomainEvents.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("acme")]
    [InlineData("  ACME  ")]
    public void ChangeSlug_TheSameAddress_IsANoOp_NoVersionBumpAndNoEvent(string same)
    {
        var workspace = NewWorkspace();
        var version = workspace.Version;

        workspace.ChangeSlug(same, _actorId, _now);

        workspace.Slug.Should().Be("acme");
        workspace.Version.Should().Be(version);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ab", "Workspaces_Workspace_SlugTooShort")]
    [InlineData("admin", "Workspaces_Workspace_SlugReserved")]
    [InlineData("Not Valid", "SharedKernel_Slug_InvalidFormat")]
    public void ChangeSlug_AnAddressBreakingTheRule_IsRefusedWithItsRuleCode_AndChangesNothing(string slug, string code)
    {
        var workspace = NewWorkspace();
        var version = workspace.Version;

        var act = () => workspace.ChangeSlug(slug, _actorId, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(code);
        workspace.Slug.Should().Be("acme");
        workspace.Version.Should().Be(version);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeSlug_TooLong_IsRefused() =>
        NewWorkspace().Invoking(w => w.ChangeSlug(new string('a', WorkspaceSlugRules.MaxLength + 1), _actorId, _now))
            .Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_SlugTooLong);

    [Fact]
    public void ChangeSlug_OnAnArchivedWorkspace_IsRefused()
    {
        var workspace = NewWorkspace();
        workspace.Archive(_actorId, _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        var act = () => workspace.ChangeSlug("acme-team", _actorId, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_CannotChangeSlugArchived);
        workspace.Slug.Should().Be("acme");
        workspace.DomainEvents.Should().BeEmpty();
    }
}
