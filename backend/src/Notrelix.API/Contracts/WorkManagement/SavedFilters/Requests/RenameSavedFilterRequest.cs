namespace Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;

public record RenameSavedFilterRequest(string Name, long ExpectedVersion);
