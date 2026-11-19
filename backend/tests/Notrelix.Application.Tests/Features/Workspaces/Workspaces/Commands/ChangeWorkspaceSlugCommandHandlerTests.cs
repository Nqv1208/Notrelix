using DomainRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ChangeWorkspaceSlug;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Workspaces.Events;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class ChangeWorkspaceSlugCommandHandlerTests : WorkspaceHandlerTestBase
{
    private ChangeWorkspaceSlugCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    private Workspace InSameAccount(string slug) =>
        Workspace.Create(TestAccountId, TestUserId, "Other", slug, TestNow);

    [Fact]
    public async Task Handle_AFreeAddress_ChangesItOnce_AndRaisesTheEventOnce()
    {
        var workspace = CreateWorkspace();
        ((Notrelix.Domain.Common.IHasDomainEvents)workspace).ClearDomainEvents();
        SetupWorkspaces(workspace);

        var result = await CreateSut().Handle(new ChangeWorkspaceSlugCommand(workspace.Id, "Acme-Team", 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        workspace.Slug.Should().Be("acme-team");
        workspace.Version.Should().Be(2);
        workspace.DomainEvents.OfType<WorkspaceSlugChangedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_TheSameAddress_IsANoOp_NoVersionBumpAndNoEvent()
    {
        var workspace = CreateWorkspace();
        ((Notrelix.Domain.Common.IHasDomainEvents)workspace).ClearDomainEvents();
        SetupWorkspaces(workspace);
        var version = workspace.Version;

        var result = await CreateSut().Handle(new ChangeWorkspaceSlugCommand(workspace.Id, " Test-Workspace ", 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        workspace.Version.Should().Be(version);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AnAddressAnotherWorkspaceOfTheAccountHolds_IsAConflictWithTheSlugTakenCode_AndChangesNothing()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace, InSameAccount("acme-team"));
        var version = workspace.Version;

        var act = () => CreateSut().Handle(new ChangeWorkspaceSlugCommand(workspace.Id, "acme-team", 1), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode
            .Should().Be(CreateWorkspaceCommandHandler.SlugTakenErrorCode);
        workspace.Slug.Should().Be("test-workspace");
        workspace.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_AnAddressOnlyAnotherAccountHolds_IsFree_BecauseAddressesAreUniquePerAccount()
    {
        var workspace = CreateWorkspace();
        var elsewhere = Workspace.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Elsewhere", "acme-team", TestNow);
        SetupWorkspaces(workspace, elsewhere);

        var result = await CreateSut().Handle(new ChangeWorkspaceSlugCommand(workspace.Id, "acme-team", 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        workspace.Slug.Should().Be("acme-team");
    }

    [Theory]
    [InlineData("ab", "Workspaces_Workspace_SlugTooShort")]
    [InlineData("settings", "Workspaces_Workspace_SlugReserved")]
    [InlineData("Not Valid", "SharedKernel_Slug_InvalidFormat")]
    public async Task Handle_AnAddressBreakingTheRule_IsRefusedWithItsRuleCode(string slug, string code)
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);

        var act = () => CreateSut().Handle(new ChangeWorkspaceSlugCommand(workspace.Id, slug, 1), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainRuleException>()).Which.RuleCode.Should().Be(code);
        workspace.Slug.Should().Be("test-workspace");
    }

    [Fact]
    public async Task Handle_AMissingWorkspace_IsNotFound()
    {
        var act = () => CreateSut().Handle(new ChangeWorkspaceSlugCommand(Guid.NewGuid(), "acme-team", 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Validator_RefusesAnEmptyAddressAndAMissingVersion()
    {
        var validator = new ChangeWorkspaceSlugCommandValidator();

        (await validator.ValidateAsync(new ChangeWorkspaceSlugCommand(Guid.NewGuid(), "", 1))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new ChangeWorkspaceSlugCommand(Guid.NewGuid(), "acme-team", 0))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new ChangeWorkspaceSlugCommand(Guid.NewGuid(), "acme-team", 1))).IsValid.Should().BeTrue();
    }
}
