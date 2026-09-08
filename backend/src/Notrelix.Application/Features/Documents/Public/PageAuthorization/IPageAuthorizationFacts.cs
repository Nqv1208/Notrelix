namespace Notrelix.Application.Features.Documents.Public.PageAuthorization;

/// <summary>
/// Producer-owned read source for page authorization facts. Consumers compose
/// this published fact into their own authorization evaluation; they never
/// query Documents persistence directly.
/// </summary>
public interface IPageAuthorizationFacts
{
    Task<PageAuthorizationFact?> ResolveAsync(Guid pageId, CancellationToken cancellationToken);
}