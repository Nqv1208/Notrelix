using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;

[IdempotencyOperation("work-management.forms.reject-form-submission.v1")]
public record RejectFormSubmissionCommand(
    Guid SubmissionId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form-submission"), SubmissionId);
}

public class RejectFormSubmissionCommandHandler : IRequestHandler<RejectFormSubmissionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectFormSubmissionCommandHandler(
        IWorkManagementDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RejectFormSubmissionCommand request, CancellationToken ct)
    {
        var submission = await _context.FormSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct);
        if (submission is null) throw new NotFoundException("FormSubmission", request.SubmissionId);

        submission.Reject(_dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
