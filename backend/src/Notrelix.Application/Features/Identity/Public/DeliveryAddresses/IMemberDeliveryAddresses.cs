namespace Notrelix.Application.Features.Identity.Public.DeliveryAddresses;

/// <summary>
/// Identity-published fact for a system sender: the address of an active, non-guest member of a Workspace, so a background job can mail a
/// person something that person asked for (the missed-mention digest, UC-CHT-H09). It exists because <see cref="Notrelix.Application.Features.Identity.Public.Queries.IIdentityUserFacts.FindEmailsAsync"/>
/// discloses another person's address only to an Owner or Admin acting for a request, which a job never is. It must only be used to send to
/// the person themself, never to show an address to anyone else; a person who is not an active non-guest member of the Workspace, or whose
/// account is not active, has none.
/// </summary>
public interface IMemberDeliveryAddresses
{
    Task<IReadOnlyDictionary<Guid, string>> FindAsync(Guid workspaceId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
