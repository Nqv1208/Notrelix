using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;

[IdempotencyOperation("work-management.forms.create-form.v1")]
public record CreateFormCommand(
    Guid BoardId,
    string Title,
    BoardVisibility? Visibility = null,
    string? SettingsJson = null,
    string? SubmitterPolicyJson = null)
    : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
}

public class CreateFormCommandHandler : IRequestHandler<CreateFormCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateFormCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateFormCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        var slug = Slug.GenerateFromName(request.Title);
        var accountId = _requestContext.RequireAccountId();
        var now = _dateTimeProvider.UtcNow;

        var form = Form.Create(
            accountId,
            board.WorkspaceId,
            request.BoardId,
            request.Title,
            slug.Value,
            _requestContext.UserId,
            now,
            request.Visibility ?? BoardVisibility.PublicLink,
            request.SettingsJson,
            request.SubmitterPolicyJson);

        _context.Forms.Add(form);
        return Result<Guid>.Success(form.Id);
    }
}
