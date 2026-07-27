using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;

public record UpdateSavedFilterFiltersRequest(List<FilterRule> Rules, long ExpectedVersion);
