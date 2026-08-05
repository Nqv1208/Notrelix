namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

public record SetBoardItemDueDateRequest(DateTime? DueDate, DateTime? StartDate);
