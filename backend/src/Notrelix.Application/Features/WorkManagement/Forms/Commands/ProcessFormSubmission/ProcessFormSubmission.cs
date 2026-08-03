using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;

[IdempotencyOperation("work-management.forms.process-form-submission.v1")]
public record ProcessFormSubmissionCommand(
    Guid SubmissionId,
    Guid CreatedItemId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.FormSubmission, SubmissionId);
}

public class ProcessFormSubmissionCommandHandler : IRequestHandler<ProcessFormSubmissionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessFormSubmissionCommandHandler(
        IWorkManagementDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ProcessFormSubmissionCommand request, CancellationToken ct)
    {
        var submission = await _context.FormSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct);
        if (submission is null) throw new NotFoundException("FormSubmission", request.SubmissionId);

        submission.MarkProcessed(request.CreatedItemId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
