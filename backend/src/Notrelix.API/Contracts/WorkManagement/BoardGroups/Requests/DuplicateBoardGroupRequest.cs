namespace Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;

/// <summary>The name of the copy, already in the person's language. Absent or blank: the server names it from the source group.</summary>
public record DuplicateBoardGroupRequest(string? Title);
