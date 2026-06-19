namespace Notrelix.API.Contracts.WorkManagement.Boards.Requests;

public record AddBoardMemberRequest(Guid UserId, string? Role = "Member");
