using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Workspaces.Public.SpacePlacement;

namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Page placement in Workspaces Spaces (docs/product/workspaces.md §79,
/// GOV-028). Documents owns <c>Page.SpaceId</c>; Workspaces publishes the target
/// Space facts; Governance decides whether the actor may place a page there —
/// evaluated with the exact contract creating a root page there uses
/// (<see cref="CreatePageCommand"/>) through the one facts provider and engine.
/// </summary>
public interface IPageSpacePlacement
{
    Task EnsureActiveTargetAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken);

    Task AuthorizeTargetAsync(Guid workspaceId, Guid? spaceId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the actor may create a page under <paramref name="parentId"/>: the right a page moved under that
    /// parent needs on the destination. A parent the actor cannot reach answers like one that does not exist.
    /// </summary>
    Task AuthorizeParentAsync(Guid workspaceId, Guid parentId, CancellationToken cancellationToken);
}

public sealed class PageSpacePlacement : IPageSpacePlacement
{
    public const string InactiveSpaceErrorCode = "workspaces.space.inactive";

    private readonly ISpacePlacementTargets _targets;
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public PageSpacePlacement(
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
            throw new ConflictException("The target space is archived; pages cannot be placed in it.", InactiveSpaceErrorCode);
    }

    public Task AuthorizeTargetAsync(Guid workspaceId, Guid? spaceId, CancellationToken cancellationToken) =>
        AuthorizeAsync(new CreatePageCommand(workspaceId, "placement", ParentId: null, SpaceId: spaceId), workspaceId, spaceId.HasValue ? "Space" : "Workspace", spaceId ?? workspaceId, cancellationToken);

    public Task AuthorizeParentAsync(Guid workspaceId, Guid parentId, CancellationToken cancellationToken) =>
        AuthorizeAsync(new CreatePageCommand(workspaceId, "placement", ParentId: parentId), workspaceId, "Page", parentId, cancellationToken);

    private async Task AuthorizeAsync(CreatePageCommand request, Guid workspaceId, string target, Guid targetId, CancellationToken cancellationToken)
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Page placement authorization requires the request execution context.");
        if (current.WorkspaceId != workspaceId)
            throw new SecurityMisconfigurationException("Page placement authorization must stay in the request workspace.");

        // Authorization descriptor only: never dispatched.
        var descriptor = _descriptors.GetRequired(typeof(CreatePageCommand));
        var snapshot = current with { Resource = null, Scope = descriptor.Scope };

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        var decision = _policy.Evaluate(descriptor, snapshot, facts, request);
        switch (decision.Kind)
        {
            case AccessDecisionKind.Allowed:
                return;
            case AccessDecisionKind.NotFound:
                throw new NotFoundException(target, targetId);
            default:
                throw new ForbiddenException(decision.Message ?? "You do not have permission to place a page there.");
        }
    }
}
