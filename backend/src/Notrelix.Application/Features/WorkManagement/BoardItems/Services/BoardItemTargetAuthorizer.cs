using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Whether the current actor may add items to a board other than the one a request is scoped to
/// (the target of a move between boards). Decided with the exact contract creating an item there
/// uses (<see cref="CreateBoardItemCommand"/>), through the one facts provider and policy engine.
/// </summary>
public interface IBoardItemTargetAuthorizer
{
    Task AuthorizeCreateAsync(Guid boardId, CancellationToken cancellationToken);
}

public sealed class BoardItemTargetAuthorizer : IBoardItemTargetAuthorizer
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public BoardItemTargetAuthorizer(
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

    public async Task AuthorizeCreateAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Target board authorization requires the request execution context.");

        // Authorization descriptor only: never dispatched.
        var request = new CreateBoardItemCommand(boardId, Guid.Empty, "target");
        var descriptor = _descriptors.GetRequired(typeof(CreateBoardItemCommand));
        var snapshot = current with
        {
            Resource = ResourceRef.Create(ResourceKind.Create("work-management.board"), boardId, current.WorkspaceId),
            Scope = ApplicationScopeKind.Resource,
        };

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        var decision = _policy.Evaluate(descriptor, snapshot, facts, request);
        switch (decision.Kind)
        {
            case AccessDecisionKind.Allowed:
                return;
            case AccessDecisionKind.NotFound:
                throw new NotFoundException("Board", boardId);
            default:
                throw new ForbiddenException(decision.Message ?? "You do not have permission to add items to that board.");
        }
    }
}
