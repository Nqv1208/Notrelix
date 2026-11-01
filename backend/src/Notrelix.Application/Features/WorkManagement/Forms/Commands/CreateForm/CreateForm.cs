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
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // A form extends the Board schema (Editor), not Board management.
    public PermissionAction Action => PermissionAction.CreateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
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

        // The slug column is unique across every tenant, so a title alone would refuse the second
        // form of that name anywhere (and tell one tenant that another uses it): an id tail makes it unique.
        string slugBase;
        try { slugBase = Slug.GenerateFromName(request.Title).Value; }
        catch (BusinessRuleException) { slugBase = "form"; }
        if (slugBase.Length > 40) slugBase = slugBase[..40].TrimEnd('-');
        var slug = Slug.Create($"{slugBase}-{Guid.NewGuid().ToString("N")[..8]}");
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
