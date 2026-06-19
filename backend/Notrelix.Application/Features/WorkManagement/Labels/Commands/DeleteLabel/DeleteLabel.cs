using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;

public record DeleteLabelCommand(Guid LabelId) : ICommand<Result>, ITransactionalRequest;

public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteLabelCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);
        _context.Labels.Remove(label);
        return Result.Success();
    }
}
