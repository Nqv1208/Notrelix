namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <param name="Name">Name of the copy, 1 to 120 characters.</param>
/// <param name="SpaceId">Space of the copy; null places it at the workspace root.</param>
/// <param name="IncludeItems">Copy the root items too (at most 500); default off.</param>
public record DuplicateBoardRequest(string Name, Guid? SpaceId, bool IncludeItems = false);
