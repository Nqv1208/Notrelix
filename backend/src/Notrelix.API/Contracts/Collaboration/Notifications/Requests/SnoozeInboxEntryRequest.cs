namespace Notrelix.API.Contracts.Collaboration.Notifications.Requests;

/// <param name="Until">When the entry returns to the inbox, unread; required, in the future and within a year.</param>
public record SnoozeInboxEntryRequest(DateTimeOffset? Until);
