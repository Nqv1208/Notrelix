using Notrelix.Application.Common.Security;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// Test-double for <see cref="IResourceAuthorizationFactsProvider"/> that resolves no resource
/// by default (returns null → callers fail closed). Useful for non-resource authorization tests.
/// </summary>
public sealed class FakeResourceAuthorizationFactsProvider : IResourceAuthorizationFactsProvider
{
    private readonly Func<ResourceRef, Guid, Task<ResourceAuthorizationFacts?>> _resolver;

    public FakeResourceAuthorizationFactsProvider(
        Func<ResourceRef, Guid, Task<ResourceAuthorizationFacts?>>? resolver = null)
    {
        _resolver = resolver ?? ((_, _) => Task.FromResult<ResourceAuthorizationFacts?>(null));
    }

    public Task<ResourceAuthorizationFacts?> ResolveAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken)
        => _resolver(resource, actorUserId);
}
