using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;

public record ConnectCalendarCommand(
    string Provider,
    string AccessToken,
    string? RefreshToken,
    Guid? WorkspaceId,
    string? SyncDirection
) : ICommand<Result<Guid>>, IAuthenticatedRequest, INoDataRequest, IGlobalRequest;

public class ConnectCalendarCommandHandler : IRequestHandler<ConnectCalendarCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(ConnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
