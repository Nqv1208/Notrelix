using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.Workspaces.Public.SpacePlacement;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// A Board or a Page is placed only in a Space of its own Workspace. A Space of another Workspace is refused with
/// the NotFound a made-up Space id gets (the same type and the same message once the id is masked), so the
/// refusal discloses nothing about the other Workspace. An archived Space of the Workspace is a Conflict.
/// </summary>
public sealed class SpacePlacementTargetTests
{
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _otherWorkspace = Guid.CreateVersion7();
    private readonly Guid _ownSpace = Guid.CreateVersion7();
    private readonly Guid _foreignSpace = Guid.CreateVersion7();
    private readonly Guid _archivedSpace = Guid.CreateVersion7();

    // The published facts answer only for the Workspace asked about, as the Workspaces implementation does.
    private ISpacePlacementTargets Targets()
    {
        var spaces = new List<(Guid Workspace, SpacePlacementTarget Target)>
        {
            (_workspace, new SpacePlacementTarget(_ownSpace, _workspace, true)),
            (_workspace, new SpacePlacementTarget(_archivedSpace, _workspace, false)),
            (_otherWorkspace, new SpacePlacementTarget(_foreignSpace, _otherWorkspace, true)),
        };
        var targets = new Mock<ISpacePlacementTargets>();
        targets.Setup(t => t.FindAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid workspaceId, Guid spaceId, CancellationToken _) =>
                spaces.Where(s => s.Workspace == workspaceId && s.Target.SpaceId == spaceId).Select(s => s.Target).SingleOrDefault());
        return targets.Object;
    }

    private BoardSpacePlacement BoardPlacement() => new(
        Targets(), Mock.Of<IRequestDescriptorRegistry>(), Mock.Of<IAccessFactsProvider>(),
        Mock.Of<IAccessPolicyEvaluator>(), Mock.Of<IExecutionContextReader>());

    private PageSpacePlacement PagePlacement() => new(
        Targets(), Mock.Of<IRequestDescriptorRegistry>(), Mock.Of<IAccessFactsProvider>(),
        Mock.Of<IAccessPolicyEvaluator>(), Mock.Of<IExecutionContextReader>());

    private static string WithoutId(string message) =>
        System.Text.RegularExpressions.Regex.Replace(message, "[0-9a-fA-F-]{32,36}", "<id>");

    [Fact]
    public async Task ABoard_CanBePlacedInASpaceOfItsOwnWorkspace() =>
        await BoardPlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, _ownSpace, CancellationToken.None))
            .Should().NotThrowAsync();

    [Fact]
    public async Task ABoard_IsRefusedASpaceOfAnotherWorkspace_WithTheNotFoundOfAMadeUpSpace()
    {
        var foreign = (await BoardPlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, _foreignSpace, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which;
        var madeUp = (await BoardPlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, Guid.CreateVersion7(), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which;

        foreign.GetType().Should().Be(madeUp.GetType());
        WithoutId(foreign.Message).Should().Be(WithoutId(madeUp.Message));
    }

    [Fact]
    public async Task ABoard_IsRefusedAnArchivedSpace_AsAConflict() =>
        (await BoardPlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, _archivedSpace, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(BoardSpacePlacement.InactiveSpaceErrorCode);

    [Fact]
    public async Task APage_CanBePlacedInASpaceOfItsOwnWorkspace() =>
        await PagePlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, _ownSpace, CancellationToken.None))
            .Should().NotThrowAsync();

    [Fact]
    public async Task APage_IsRefusedASpaceOfAnotherWorkspace_WithTheNotFoundOfAMadeUpSpace()
    {
        var foreign = (await PagePlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, _foreignSpace, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which;
        var madeUp = (await PagePlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, Guid.CreateVersion7(), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()).Which;

        foreign.GetType().Should().Be(madeUp.GetType());
        WithoutId(foreign.Message).Should().Be(WithoutId(madeUp.Message));
    }

    [Fact]
    public async Task APage_IsRefusedAnArchivedSpace_AsAConflict() =>
        (await PagePlacement().Invoking(p => p.EnsureActiveTargetAsync(_workspace, _archivedSpace, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(PageSpacePlacement.InactiveSpaceErrorCode);
}
