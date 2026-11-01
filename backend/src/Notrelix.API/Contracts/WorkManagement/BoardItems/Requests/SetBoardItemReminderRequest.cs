namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>Minutes before the due time: -1 off, 0, 15, 60, 1440 (default), 2880 or 10080.</summary>
public record SetBoardItemReminderRequest(int OffsetMinutes, long ExpectedVersion);
