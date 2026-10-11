using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Authorizes the destination of a Space move (GOV-028: ManageSpaces on the
/// target parent, or on the Workspace for the root). The pipeline already
/// authorized the moved Space itself; the destination is evaluated with the
/// exact contract that creating a Space there uses (<see cref="CreateSpaceCommand"/>),
/// through the one facts provider and policy engine — no second evaluator.
/// </summary>
public interface ISpaceTargetAuthorizer
{
    Task AuthorizePlacementAsync(Guid workspaceId, Guid? targetParentSpaceId, CancellationToken cancellationToken);
}

public sealed class SpaceTargetAuthorizer : ISpaceTargetAuthorizer
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public SpaceTargetAuthorizer(
        IRequestDescriptorRegistry descriptors,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy,
        IExecutionContextReader executionContext)
    {
        _descriptors = descriptors;
        _factsProvider = factsProvider;
        _policy = policy;
        _executionContext = executionContext;
    }

    public async Task AuthorizePlacementAsync(Guid workspaceId, Guid? targetParentSpaceId, CancellationToken cancellationToken)
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Space placement authorization requires the request execution context.");
        if (current.WorkspaceId != workspaceId)
            throw new SecurityMisconfigurationException("Space placement authorization must stay in the request workspace.");

        // Authorization descriptor only: never dispatched.
        var request = new CreateSpaceCommand(workspaceId, "placement", ParentSpaceId: targetParentSpaceId);
        var descriptor = _descriptors.GetRequired(typeof(CreateSpaceCommand));
        var snapshot = current with { Resource = null };

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        var decision = _policy.Evaluate(descriptor, snapshot, facts, request);
        switch (decision.Kind)
        {
            case AccessDecisionKind.Allowed:
                return;
            case AccessDecisionKind.NotFound:
                throw new NotFoundException(nameof(Space), targetParentSpaceId ?? workspaceId);
            default:
                throw new ForbiddenException(decision.Message ?? "You do not have permission to place a space there.");
        }
    }
}
