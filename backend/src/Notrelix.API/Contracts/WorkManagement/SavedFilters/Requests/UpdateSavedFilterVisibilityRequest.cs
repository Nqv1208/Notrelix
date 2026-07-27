using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;

public record UpdateSavedFilterVisibilityRequest(SavedFilterVisibility Visibility, long ExpectedVersion);
