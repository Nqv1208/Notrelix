using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Labels.RemoveLabelFromCard;
using global::Notrelix.Application.Features.Boards.Commands.Labels;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Labels.RemoveLabelFromCard;

public record RemoveLabelFromCardCommand(Guid CardId, Guid LabelId) : IRequest<Result>;

public class RemoveLabelFromCardCommandHandler : IRequestHandler<RemoveLabelFromCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public RemoveLabelFromCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveLabelFromCardCommand request, CancellationToken ct)
    {
        var cl = await _context.CardLabels
            .FirstOrDefaultAsync(l => l.CardId == request.CardId && l.LabelId == request.LabelId, ct);
        if (cl is not null)
        {
            _context.CardLabels.Remove(cl);
            await _context.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}
