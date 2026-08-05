using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;

public record UpdateBoardPreferenceGroupRequest(GroupRule? Group);
