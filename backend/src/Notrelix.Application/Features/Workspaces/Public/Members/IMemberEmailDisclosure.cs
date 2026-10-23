namespace Notrelix.Application.Features.Workspaces.Public.Members;

/// <summary>
/// The one Workspaces rule for showing a member's email (accepted 2026-10-23): only a Workspace Owner or
/// Admin sees email, only of active non-Guest members of the same Workspace; every other reader, and any
/// Guest subject, gets nothing. The address itself comes from the Identity public facts port.
/// </summary>
public interface IMemberEmailDisclosure
{
    /// <summary>Emails of the subjects the calling principal may see; absent subjects are omitted.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(Guid workspaceId, IReadOnlyCollection<Guid> subjectIds, CancellationToken ct);
}
