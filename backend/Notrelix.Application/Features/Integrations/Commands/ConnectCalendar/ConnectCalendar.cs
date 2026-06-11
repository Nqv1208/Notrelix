using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Integrations.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Integrations.Commands.ConnectCalendar;

public record ConnectCalendarCommand(
    string Provider,
    string AccessToken,
    string? RefreshToken,
    Guid? WorkspaceId,
    string? SyncDirection
) : IRequest<Result<Guid>>;

public class ConnectCalendarCommandHandler : IRequestHandler<ConnectCalendarCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(ConnectCalendarCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
