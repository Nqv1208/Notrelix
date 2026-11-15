using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <summary>
/// Whether the caller may edit a page: the decision the pipeline makes for a write of that page
/// (<c>UpdatePage</c>, the action of rename and every block write), through the one facts provider and
/// policy engine. Derived state for clients (a read-only editor); the server still authorizes every write.
/// </summary>
public interface IPageEditAccess
{
    Task<bool> CanEditAsync(Guid pageId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IPageEditAccess"/>
public sealed class PageEditAccess : IPageEditAccess
{
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IExecutionContextReader _executionContext;

    public PageEditAccess(
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

    public async Task<bool> CanEditAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var current = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Page edit access requires the request execution context.");
        var resource = ResourceRef.Create(ResourceKind.Create("documents.page"), pageId);

        // The authorization descriptor of renaming a page; the command itself is never dispatched.
        var request = new UpdatePageCommand(pageId, string.Empty, 0);
        var descriptor = _descriptors.GetRequired(typeof(UpdatePageCommand));
        var snapshot = current with { Resource = resource, Scope = ApplicationScopeKind.Resource };

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        return _policy.Evaluate(descriptor, snapshot, facts, request).Kind == AccessDecisionKind.Allowed;
    }
}
