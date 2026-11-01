using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

namespace Notrelix.Application.Features.Collaboration.Comments.Services;

public enum CommentTargetRight
{
    View,
    Edit,
}

/// <summary>
/// Whether the current actor holds a right on the item or page a comment sits on. Decided by the one
/// facts provider and policy engine, exactly as the pipeline decides a request on that target.
/// </summary>
public interface ICommentTargetAccess
{
    Task<bool> HasAsync(ResourceRef target, CommentTargetRight right, CancellationToken cancellationToken);
}

public sealed class CommentTargetAccess : ICommentTargetAccess
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;
    private readonly IWorkItemNotificationFacts _itemFacts;

    public CommentTargetAccess(
        IRequestDescriptorRegistry descriptors,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy,
        IExecutionContextReader executionContext,
        IWorkItemNotificationFacts itemFacts)
    {
        _descriptors = descriptors;
        _factsProvider = factsProvider;
        _policy = policy;
        _executionContext = executionContext;
        _itemFacts = itemFacts;
    }

    public async Task<bool> HasAsync(ResourceRef target, CommentTargetRight right, CancellationToken cancellationToken)
    {
        var action = (target.Kind.Value, right) switch
        {
            ("work-management.board-item", CommentTargetRight.View) => PermissionAction.ViewBoard,
            ("work-management.board-item", CommentTargetRight.Edit) => PermissionAction.UpdateItem,
            ("documents.page", CommentTargetRight.View) => PermissionAction.ViewPage,
            ("documents.page", CommentTargetRight.Edit) => PermissionAction.UpdatePage,
            _ => (PermissionAction?)null,
        };

        // A comment on any other kind has no view/edit definition: fail closed.
        if (action is null)
        {
            return false;
        }

        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Comment target access requires the request execution context.");

        // A deleted board item is gone for its comments and attachments, as a trashed page is: the Work
        // Management-published item fact is null once the item no longer exists in the Workspace.
        if (target.Kind.Value == "work-management.board-item"
            && await _itemFacts.FindAsync(current.WorkspaceId ?? Guid.Empty, target.ResourceId, null, cancellationToken) is null)
        {
            return false;
        }

        // Same permission-only probe the pipeline uses for additional actions; the request is never dispatched.
        var descriptor = _descriptors.GetRequired(typeof(UpdateCommentCommand))
            with
        { Access = new AccessRequirements(true, false, false, false) };
        var snapshot = current with { Resource = target, Scope = ApplicationScopeKind.Resource };
        var probe = new TargetProbe(action.Value, target);

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, probe, cancellationToken);
        return _policy.Evaluate(descriptor, snapshot, facts, probe).Kind == AccessDecisionKind.Allowed;
    }

    private sealed class TargetProbe(PermissionAction action, ResourceRef? resource) : IRequirePermission
    {
        public PermissionAction Action { get; } = action;
        public ResourceRef? Resource { get; } = resource;
    }
}
