using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Whether the current actor may apply a page command to every page a subtree cascade touches. The command's
/// pipeline authorization covers the page it names; a child can carry narrower access of its own, so each
/// page the cascade would change is decided with the same command contract (trash, restore or delete) through
/// the one facts provider and policy engine. One page that is not allowed refuses the whole cascade.
/// </summary>
public interface IPageSubtreeAuthorizer
{
    /// <summary>
    /// Throws a <see cref="ForbiddenException"/> with <see cref="SubtreeNotPermittedCode"/> when any page is
    /// not allowed. The answer is generic: it names no page and gives no count, so a page the actor cannot
    /// even view is never revealed.
    /// </summary>
    Task EnsureAllowedAsync<TRequest>(IEnumerable<Guid> pageIds, Func<Guid, TRequest> commandFor, CancellationToken cancellationToken)
        where TRequest : notnull;
}

public sealed class PageSubtreeAuthorizer : IPageSubtreeAuthorizer
{
    public const string SubtreeNotPermittedCode = "documents.page.subtree-not-permitted";

    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public PageSubtreeAuthorizer(
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

    public async Task EnsureAllowedAsync<TRequest>(IEnumerable<Guid> pageIds, Func<Guid, TRequest> commandFor, CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Page subtree authorization requires the request execution context.");
        var descriptor = _descriptors.GetRequired(typeof(TRequest));

        foreach (var pageId in pageIds)
        {
            // Authorization descriptor only: the command is never dispatched.
            var request = commandFor(pageId);
            var snapshot = current with
            {
                Resource = ResourceRef.Create(ResourceKind.Create("documents.page"), pageId, current.WorkspaceId),
                Scope = ApplicationScopeKind.Resource,
            };

            var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
            // The first refused page ends the check: nothing has been written yet.
            if (_policy.Evaluate(descriptor, snapshot, facts, request).Kind != AccessDecisionKind.Allowed)
                throw new ForbiddenException(
                    "Some pages under this page cannot be changed by you, so nothing was changed.",
                    SubtreeNotPermittedCode);
        }
    }
}
