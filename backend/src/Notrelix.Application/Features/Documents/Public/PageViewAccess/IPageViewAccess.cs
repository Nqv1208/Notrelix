namespace Notrelix.Application.Features.Documents.Public.PageViewAccess;

/// <summary>
/// Documents-published fact: whether a person may see a page. It is the same decision that reading the page
/// makes for that person (ViewPage over the page, through the one facts provider and policy engine), asked on
/// behalf of someone other than the caller, for example before telling them about a mention in the page.
/// A page or person that cannot be resolved is not viewable.
/// </summary>
public interface IPageViewAccess
{
    Task<bool> CanViewAsync(Guid accountId, Guid workspaceId, Guid pageId, Guid userId, CancellationToken cancellationToken);
}
