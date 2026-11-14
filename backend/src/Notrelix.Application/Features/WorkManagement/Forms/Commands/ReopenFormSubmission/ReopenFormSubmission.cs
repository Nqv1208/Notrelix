using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.ReopenFormSubmission;

/// <summary>Returns a rejected, spam or deleted response to the New tab: the undo of those decisions.</summary>
[IdempotencyOperation("work-management.forms.reopen-form-submission.v1")]
public record ReopenFormSubmissionCommand(
    Guid SubmissionId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form-submission"), SubmissionId);
}

public class ReopenFormSubmissionCommandHandler : IRequestHandler<ReopenFormSubmissionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;

    public ReopenFormSubmissionCommandHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ReopenFormSubmissionCommand request, CancellationToken ct)
    {
        var submission = await _context.FormSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct);
        if (submission is null) throw new NotFoundException("FormSubmission", request.SubmissionId);

        submission.Reopen();

        return Result.Success();
    }
}
