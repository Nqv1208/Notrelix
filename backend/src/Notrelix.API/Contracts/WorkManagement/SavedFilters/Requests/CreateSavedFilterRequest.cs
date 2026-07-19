using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;

public record CreateSavedFilterRequest(string Name, List<FilterRule>? Rules = null);
