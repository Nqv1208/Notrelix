using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Labels.CreateLabel;
using global::Notrelix.Application.Features.WorkManagement.Commands.Labels;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.CreateLabel;

public record CreateLabelCommand(Guid BoardId, string Color, string? Name) : IRequest<Result<Guid>>;

public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateLabelCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateLabelCommand request, CancellationToken ct)
    {
        var label = Label.Create(request.BoardId, request.Color, request.Name);
        _context.Labels.Add(label);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(label.Id);
    }
}
