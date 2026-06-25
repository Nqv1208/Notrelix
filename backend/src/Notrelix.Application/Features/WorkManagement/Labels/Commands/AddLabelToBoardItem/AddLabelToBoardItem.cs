using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;

public record AddLabelToBoardItemCommand(Guid BoardItemId, Guid LabelId) : ICommand<Result>, ITransactionalRequest;

public class AddLabelToBoardItemCommandHandler : IRequestHandler<AddLabelToBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddLabelToBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddLabelToBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);

        var exists = await _context.BoardItemLabels
            .AnyAsync(l => l.ItemId == request.BoardItemId && l.LabelId == request.LabelId, ct);
        if (exists) return Result.Success();

        var link = BoardItemLabel.Create(
            card.WorkspaceId, label.BoardId, request.BoardItemId, request.LabelId,
            _currentUser.UserId, _dateTimeProvider.UtcNow);
        _context.BoardItemLabels.Add(link);
        return Result.Success();
    }
}
