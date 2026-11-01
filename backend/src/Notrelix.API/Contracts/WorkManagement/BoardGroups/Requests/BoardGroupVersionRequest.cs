namespace Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;

/// <summary>Body of the group writes that act on the group as a whole (archive, unarchive): the group version the person saw. A stale version is refused and changes nothing.</summary>
public record BoardGroupVersionRequest(long ExpectedVersion);
