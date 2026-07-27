using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;

public record CreateOrUpdateBoardPreferenceRequest(
    List<FilterRule>? Filters = null,
    List<SortRule>? Sorts = null,
    GroupRule? Group = null);
