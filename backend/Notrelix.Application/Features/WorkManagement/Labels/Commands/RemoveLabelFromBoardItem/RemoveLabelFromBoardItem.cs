using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

public record RemoveLabelFromCardCommand(Guid BoardItemId, Guid LabelId) : ICommand<Result>, ITransactionalRequest;

public class RemoveLabelFromCardCommandHandler : IRequestHandler<RemoveLabelFromCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public RemoveLabelFromCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveLabelFromCardCommand request, CancellationToken ct)
    {
        var cl = await _context.BoardItemLabels
            .FirstOrDefaultAsync(l => l.ItemId == request.BoardItemId && l.LabelId == request.LabelId, ct);
        if (cl is not null)
        {
            _context.BoardItemLabels.Remove(cl);
        }
        return Result.Success();
    }
}
