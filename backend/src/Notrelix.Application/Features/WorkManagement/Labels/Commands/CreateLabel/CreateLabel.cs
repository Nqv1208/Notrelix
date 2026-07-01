using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;

public record CreateLabelCommand(Guid BoardId, string Color, string? Name) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateLabelCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateLabelCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        var label = Label.Create(Guid.Empty, board.WorkspaceId, request.BoardId, request.Name ?? "", LabelColor.Create(request.Color), _currentUser.UserId, _dateTimeProvider.UtcNow);
        _context.Labels.Add(label);
        return Result<Guid>.Success(label.Id);
    }
}
