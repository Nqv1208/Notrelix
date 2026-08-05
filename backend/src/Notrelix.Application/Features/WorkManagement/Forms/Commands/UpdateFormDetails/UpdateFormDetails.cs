using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;

[IdempotencyOperation("work-management.forms.update-form-details.v1")]
public record UpdateFormDetailsCommand(
    Guid FormId,
    string Title,
    BoardVisibility Visibility,
    string SettingsJson,
    string SubmitterPolicyJson,
    long? ExpectedVersion = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateFormDetailsCommandHandler : IRequestHandler<UpdateFormDetailsCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateFormDetailsCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateFormDetailsCommand request, CancellationToken ct)
    {
        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null) throw new NotFoundException("Form", request.FormId);

        form.UpdateDetails(
            request.Title,
            request.Visibility,
            request.SettingsJson,
            request.SubmitterPolicyJson,
            _requestContext.UserId,
            _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
