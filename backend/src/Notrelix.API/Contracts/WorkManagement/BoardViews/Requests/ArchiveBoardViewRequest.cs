namespace Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;

/// <summary>Archive a view; when it is the default, the view that takes over (absent: the oldest shared one).</summary>
public record ArchiveBoardViewRequest(long ExpectedVersion, Guid? NewDefaultViewId = null);
