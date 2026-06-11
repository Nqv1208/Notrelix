using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Labels.RemoveLabelFromBoardItem;
using global::Notrelix.Application.Features.WorkManagement.Commands.Labels;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.RemoveLabelFromBoardItem;

public record RemoveLabelFromCardCommand(Guid BoardItemId, Guid LabelId) : IRequest<Result>;

public class RemoveLabelFromCardCommandHandler : IRequestHandler<RemoveLabelFromCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public RemoveLabelFromCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveLabelFromCardCommand request, CancellationToken ct)
    {
        var cl = await _context.BoardItemLabels
            .FirstOrDefaultAsync(l => l.BoardItemId == request.BoardItemId && l.LabelId == request.LabelId, ct);
        if (cl is not null)
        {
            _context.BoardItemLabels.Remove(cl);
            await _context.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}
