using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands;
using global::Notrelix.Application.Features.WorkManagement.Commands.Labels.AddLabelToBoardItem;
using global::Notrelix.Application.Features.WorkManagement.Commands.Labels;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.AddLabelToBoardItem;

public record AddLabelToCardCommand(Guid BoardItemId, Guid LabelId) : IRequest<Result>;

public class AddLabelToCardCommandHandler : IRequestHandler<AddLabelToCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public AddLabelToCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(AddLabelToCardCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .Include(c => c.Labels)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        card.AddLabel(request.LabelId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
