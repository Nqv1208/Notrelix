namespace Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;

/// <summary>The version of the view the person saw; a stale one changes nothing.</summary>
public record BoardViewVersionRequest(long ExpectedVersion);
