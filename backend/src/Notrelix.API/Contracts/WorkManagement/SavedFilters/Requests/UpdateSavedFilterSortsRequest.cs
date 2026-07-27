using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;

public record UpdateSavedFilterSortsRequest(List<SortRule> SortRules, long ExpectedVersion);
