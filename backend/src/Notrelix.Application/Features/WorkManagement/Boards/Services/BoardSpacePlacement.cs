using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.Workspaces.Public.SpacePlacement;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// Board placement in Workspaces Spaces (docs/product/workspaces.md §79,
/// GOV-028). Work Management owns <c>Board.SpaceId</c>; Workspaces publishes the
/// target Space facts; Governance decides whether the actor may place a Board
/// there — evaluated with the exact contract creating a Board there uses
/// (<see cref="CreateBoardInWorkspaceCommand"/>) through the one facts
/// provider and policy engine.
/// </summary>
public interface IBoardSpacePlacement
{
    /// <summary>The Space exists in the Workspace and is active, otherwise NotFound / Conflict.</summary>
    Task EnsureActiveTargetAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>The current actor may create a Board in the target (Space, or the Workspace root for null).</summary>
    Task AuthorizeTargetAsync(Guid workspaceId, Guid? spaceId, CancellationToken cancellationToken);
}

public sealed class BoardSpacePlacement : IBoardSpacePlacement
{
    public const string InactiveSpaceErrorCode = "workspaces.space.inactive";

    private readonly ISpacePlacementTargets _targets;
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public BoardSpacePlacement(
        ISpacePlacementTargets targets,
        IRequestDescriptorRegistry descriptors,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy,
        IExecutionContextReader executionContext)
    {
        _targets = targets;
        _descriptors = descriptors;
        _factsProvider = factsProvider;
        _policy = policy;
        _executionContext = executionContext;
    }

    public async Task EnsureActiveTargetAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var target = await _targets.FindAsync(workspaceId, spaceId, cancellationToken)
            ?? throw new NotFoundException("Space", spaceId);
        if (!target.IsActive)
            throw new ConflictException("The target space is archived; boards cannot be placed in it.", InactiveSpaceErrorCode);
    }

    public async Task AuthorizeTargetAsync(Guid workspaceId, Guid? spaceId, CancellationToken cancellationToken)
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Board placement authorization requires the request execution context.");
        if (current.WorkspaceId != workspaceId)
            throw new SecurityMisconfigurationException("Board placement authorization must stay in the request workspace.");

        // Authorization descriptor only: never dispatched.
        var request = new CreateBoardInWorkspaceCommand(workspaceId, "placement", null, null, null, spaceId);
        var descriptor = _descriptors.GetRequired(typeof(CreateBoardInWorkspaceCommand));
        var snapshot = current with { Resource = null, Scope = descriptor.Scope };

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        var decision = _policy.Evaluate(descriptor, snapshot, facts, request);
        switch (decision.Kind)
        {
            case AccessDecisionKind.Allowed:
                return;
            case AccessDecisionKind.NotFound:
                throw new NotFoundException("Space", spaceId ?? workspaceId);
            default:
                throw new ForbiddenException(decision.Message ?? "You do not have permission to place a board there.");
        }
    }
}
