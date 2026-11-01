namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <summary>
/// Body of the board lifecycle writes that act on the board as a whole (archive, unarchive, restore): the
/// board version the person saw. A stale version is refused and changes nothing.
/// </summary>
public record BoardVersionRequest(long ExpectedVersion);
