using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.MarkFormSubmissionAsSpam;

[IdempotencyOperation("work-management.forms.mark-form-submission-as-spam.v1")]
public record MarkFormSubmissionAsSpamCommand(
    Guid SubmissionId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form-submission"), SubmissionId);
}

public class MarkFormSubmissionAsSpamCommandHandler : IRequestHandler<MarkFormSubmissionAsSpamCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkFormSubmissionAsSpamCommandHandler(
        IWorkManagementDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MarkFormSubmissionAsSpamCommand request, CancellationToken ct)
    {
        var submission = await _context.FormSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct);
        if (submission is null) throw new NotFoundException("FormSubmission", request.SubmissionId);

        submission.MarkAsSpam(_dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
