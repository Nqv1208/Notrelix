using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;

[IdempotencyOperation("work-management.forms.restore-form.v1")]
public record RestoreFormCommand(
    Guid FormId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public class RestoreFormCommandHandler : IRequestHandler<RestoreFormCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreFormCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreFormCommand request, CancellationToken ct)
    {
        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null) throw new NotFoundException("Form", request.FormId);

        form.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
