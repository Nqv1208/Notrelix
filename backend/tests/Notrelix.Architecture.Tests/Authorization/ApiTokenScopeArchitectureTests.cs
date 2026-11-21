using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Architecture.Tests.Authorization;

/// <summary>
/// An API token reaches only the workspace and resource requests its scopes cover, and the scope is decided in one
/// place (<see cref="ApiTokenScopeMap"/>, read into the request descriptor). Every authenticated request a token can
/// reach therefore maps to exactly one scope, or is explicitly refused to every token; a new request with no mapping
/// fails here instead of reaching tokens (or silently none).
/// </summary>
public sealed class ApiTokenScopeArchitectureTests
{
    private static readonly ApplicationScopeKind[] TokenReachableScopes =
    [
        ApplicationScopeKind.Workspace,
        ApplicationScopeKind.Resource,
    ];

    private static IReadOnlyList<RequestDescriptor> TokenReachable()
    {
        var registry = RequestDescriptorRegistry.Create(typeof(ICommand<>).Assembly);
        return registry.Descriptors
            .Where(d => d.Principal == ApplicationPrincipalKind.Authenticated && TokenReachableScopes.Contains(d.Scope))
            .ToList();
    }

    [Fact]
    public void EveryWorkspaceScopedRequest_MapsToExactlyOneScope_OrIsExplicitlyRefusedToTokens()
    {
        var requests = TokenReachable();
        requests.Count.Should().BeGreaterThan(300, "an empty scan proves nothing");

        var unmapped = requests
            .Where(d => ApiTokenScopeMap.ContextOf(d.RequestType) is not { } context
                || !(ApiTokenScopeMap.ScopedContexts.ContainsKey(context) || ApiTokenScopeMap.RefusedContexts.Contains(context)))
            .Select(d => d.RequestType.FullName)
            .ToList();
        unmapped.Should().BeEmpty(
            "a context must be named in ApiTokenScopeMap.ScopedContexts (mapped) or RefusedContexts (refused to tokens): "
            + string.Join("; ", unmapped));

        foreach (var descriptor in requests)
        {
            var context = ApiTokenScopeMap.ContextOf(descriptor.RequestType)!;
            if (ApiTokenScopeMap.WorkspaceReadRequests.Contains(descriptor.RequestType.FullName!))
            {
                descriptor.ApiTokenScope.Should().Be(ApiTokenScopes.WorkspaceRead, descriptor.RequestType.FullName);
            }
            else if (ApiTokenScopeMap.ScopedContexts.TryGetValue(context, out var prefix))
            {
                var expected = descriptor.Kind == ApplicationRequestKind.Query && descriptor.DataAccess == ApplicationDataAccessKind.Read
                    ? $"{prefix}:read"
                    : $"{prefix}:write";
                descriptor.ApiTokenScope.Should().Be(expected, descriptor.RequestType.FullName);
                ApiTokenScopes.Vocabulary.Should().Contain(descriptor.ApiTokenScope!, descriptor.RequestType.FullName);
            }
            else
            {
                descriptor.ApiTokenScope.Should().BeNull(
                    $"{descriptor.RequestType.FullName} is in a context refused to every token");
            }
        }
    }

    [Fact]
    public void TheMappedAndTheRefusedContexts_AreDisjoint_AndEveryRefusedContextIsARealFeatureFolder()
    {
        ApiTokenScopeMap.ScopedContexts.Keys.Should().NotIntersectWith(ApiTokenScopeMap.RefusedContexts);

        var contexts = typeof(ICommand<>).Assembly.GetTypes()
            .Select(ApiTokenScopeMap.ContextOf)
            .Where(c => c is not null)
            .ToHashSet();

        ApiTokenScopeMap.RefusedContexts.Concat(ApiTokenScopeMap.ScopedContexts.Keys)
            .Should().OnlyContain(c => contexts.Contains(c), "a renamed or removed context must leave the map");
    }

    [Fact]
    public void TheWorkspaceReadSet_IsAnExplicitList_OfExistingReadOnlyQueries()
    {
        var byName = TokenReachable().ToDictionary(d => d.RequestType.FullName!);

        ApiTokenScopeMap.WorkspaceReadRequests.Should().HaveCountLessThanOrEqualTo(10, "the set is small and named");
        foreach (var name in ApiTokenScopeMap.WorkspaceReadRequests)
        {
            byName.Should().ContainKey(name, "a name in the workspace:read set must be a real workspace-scoped request");
            byName[name].Kind.Should().Be(ApplicationRequestKind.Query, name);
            byName[name].DataAccess.Should().Be(ApplicationDataAccessKind.Read, name);
        }
    }

    [Fact]
    public void EveryScopeOfTheVocabulary_IsNeededBySomeRequest()
    {
        var needed = TokenReachable().Select(d => d.ApiTokenScope).Where(s => s is not null).ToHashSet();

        ApiTokenScopes.Vocabulary.Should().OnlyContain(scope => needed.Contains(scope), "a scope no request needs grants nothing");
    }

    [Fact]
    public void AReadScope_IsNeverNeededByARequestThatChangesData()
    {
        var offenders = TokenReachable()
            .Where(d => d.ApiTokenScope is { } s && s.EndsWith(":read", StringComparison.Ordinal)
                && (d.Kind == ApplicationRequestKind.Command || d.DataAccess != ApplicationDataAccessKind.Read))
            .Select(d => d.RequestType.FullName)
            .ToList();

        offenders.Should().BeEmpty("a command, or a query that writes, needs a write scope: " + string.Join("; ", offenders));
    }
}
