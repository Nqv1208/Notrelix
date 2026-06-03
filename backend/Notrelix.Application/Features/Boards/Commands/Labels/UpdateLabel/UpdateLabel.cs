using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Labels.UpdateLabel;
using global::Notrelix.Application.Features.Boards.Commands.Labels;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Labels.UpdateLabel;

public record UpdateLabelCommand(Guid LabelId, string? Name, string? Color) : IRequest<Result>;

public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateLabelCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);
        if (request.Name is not null) label.UpdateName(request.Name);
        if (request.Color is not null) label.UpdateColor(request.Color);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
