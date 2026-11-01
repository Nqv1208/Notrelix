namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

/// <summary>PATCH /boards/{boardId}/members/{userId}/role: the new role and the board version the person saw.</summary>
public record UpdateBoardMemberRoleRequest(string Role, long ExpectedVersion);
