using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;

[IdempotencyOperation("work-management.forms.close-form.v1")]
public record CloseFormCommand(
    Guid FormId,
    long? ExpectedVersion = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Form, FormId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class CloseFormCommandHandler : IRequestHandler<CloseFormCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CloseFormCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CloseFormCommand request, CancellationToken ct)
    {
        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null) throw new NotFoundException("Form", request.FormId);

        form.Close(_requestContext.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
