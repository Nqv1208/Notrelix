using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;

public record UpdateSavedFilterGroupRequest(GroupRule? GroupRule, long ExpectedVersion);
