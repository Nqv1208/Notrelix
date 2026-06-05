using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.Commands.Labels.AddLabelToCard;
using global::Notrelix.Application.Features.Boards.Commands.Labels;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Labels.AddLabelToCard;

public record AddLabelToCardCommand(Guid CardId, Guid LabelId) : IRequest<Result>;

public class AddLabelToCardCommandHandler : IRequestHandler<AddLabelToCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public AddLabelToCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(AddLabelToCardCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.Labels)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.AddLabel(request.LabelId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
