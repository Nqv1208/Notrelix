using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;

namespace Notrelix.Application.Features.Documents.Pages.Services;

/// <inheritdoc cref="IPageViewAccess"/>
public sealed class PageViewAccess : IPageViewAccess
{
    private readonly IResourceLocator _resourceLocator;
    private readonly IRequestDescriptorRegistry _descriptors;
    private readonly IAccessFactsProvider _factsProvider;
    private readonly IAccessPolicyEvaluator _policy;

    public PageViewAccess(
        IResourceLocator resourceLocator,
        IRequestDescriptorRegistry descriptors,
        IAccessFactsProvider factsProvider,
        IAccessPolicyEvaluator policy)
    {
        _resourceLocator = resourceLocator;
        _descriptors = descriptors;
        _factsProvider = factsProvider;
        _policy = policy;
    }

    public async Task<bool> CanViewAsync(Guid accountId, Guid workspaceId, Guid pageId, Guid userId, CancellationToken cancellationToken)
    {
        var resource = ResourceRef.Create(ResourceKind.Create("documents.page"), pageId, workspaceId);
        var location = await _resourceLocator.LocateAsync(resource, userId, cancellationToken);
        if (location is null || location.AccountId != accountId || location.WorkspaceId != workspaceId) return false;

        // The authorization descriptor of reading a page; the query itself is never dispatched.
        var request = new GetPageQuery(pageId);
        var descriptor = _descriptors.GetRequired(typeof(GetPageQuery));
        var snapshot = new ExecutionContextSnapshot(
            userId,
            location.AccountId,
            location.WorkspaceId,
            resource,
            ApplicationPrincipalKind.Authenticated,
            ApplicationScopeKind.Resource,
            Guid.NewGuid().ToString("D"));

        var facts = await _factsProvider.ResolveAsync(descriptor, snapshot, request, cancellationToken);
        return _policy.Evaluate(descriptor, snapshot, facts, request).Kind == AccessDecisionKind.Allowed;
    }
}
