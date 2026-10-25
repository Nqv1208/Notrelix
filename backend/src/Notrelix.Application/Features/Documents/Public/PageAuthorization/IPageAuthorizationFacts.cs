namespace Notrelix.Application.Features.Documents.Public.PageAuthorization;

/// <summary>
/// Producer-owned read source for page authorization facts. Consumers compose
/// this published fact into their own authorization evaluation; they never
/// query Documents persistence directly.
/// </summary>
public interface IPageAuthorizationFacts
{
    Task<PageAuthorizationFact?> ResolveAsync(Guid pageId, CancellationToken cancellationToken);

    /// <summary>
    /// The page a live block belongs to, or null when the block does not exist.
    /// A block is authorized on its page.
    /// </summary>
    Task<Guid?> ResolveBlockPageIdAsync(Guid blockId, CancellationToken cancellationToken);
}