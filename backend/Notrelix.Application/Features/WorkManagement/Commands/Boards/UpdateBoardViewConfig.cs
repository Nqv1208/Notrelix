using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record UpdateBoardViewConfigCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    string ConfigJson) : IRequest<BoardViewDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.UpdateBoardView;
}

public class UpdateBoardViewConfigCommandHandler : IRequestHandler<UpdateBoardViewConfigCommand, BoardViewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardViewConfigCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BoardViewDto> Handle(UpdateBoardViewConfigCommand request, CancellationToken cancellationToken)
    {
        var view = await _context.BoardViews
            .FirstOrDefaultAsync(v => v.Id == request.ViewId && v.BoardId == request.BoardId, cancellationToken);

        if (view == null)
            throw new NotFoundException("BoardView", request.ViewId);

        var config = BoardViewConfig.Create(JsonValue.Create(request.ConfigJson));
        view.UpdateConfig(config, _currentUser.UserId, _dateTimeProvider.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return new BoardViewDto(
            view.Id,
            view.BoardId,
            view.Name,
            view.Type.ToString(),
            view.Config.Data.Value,
            view.IsDefault
        );
    }
}
