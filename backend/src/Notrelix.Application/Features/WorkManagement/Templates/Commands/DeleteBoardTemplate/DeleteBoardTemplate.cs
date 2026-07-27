using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.DeleteBoardTemplate;

public record DeleteBoardTemplateCommand(Guid TemplateId, string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, TemplateId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"delete-board-template:{TemplateId}";
}

public class DeleteBoardTemplateCommandHandler : IRequestHandler<DeleteBoardTemplateCommand, Result>
{
    private readonly IWorkManagementDbContext _context;

    public DeleteBoardTemplateCommandHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteBoardTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.BoardTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        if (template is null) throw new NotFoundException(nameof(BoardTemplate), request.TemplateId);

        _context.BoardTemplates.Remove(template);
        return Result.Success();
    }
}
