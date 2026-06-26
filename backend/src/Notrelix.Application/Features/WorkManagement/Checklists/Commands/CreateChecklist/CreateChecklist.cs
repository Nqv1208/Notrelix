using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

public record CreateChecklistCommand(Guid BoardItemId, string Title) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateChecklistCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateChecklistCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);

        if (item is null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var position = FractionalIndex.Initial();
        var checklist = Checklist.Create(item.WorkspaceId, request.BoardItemId, request.Title, position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        _context.Checklists.Add(checklist);
        return Result<Guid>.Success(checklist.Id);
    }
}
