using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;

[IdempotencyOperation("work-management.forms.update-form-question.v1")]
public record UpdateFormQuestionCommand(
    Guid FormId,
    Guid QuestionId,
    string Label,
    bool IsRequired,
    string? ConfigJson,
    string? Position,
    Guid? BoardFieldId = null,
    long? ExpectedVersion = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateFormQuestionCommandHandler : IRequestHandler<UpdateFormQuestionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateFormQuestionCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateFormQuestionCommand request, CancellationToken ct)
    {
        var form = await _context.Forms.Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null || form.IsDeleted) throw new NotFoundException("Form", request.FormId);

        var question = form.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question is null) throw new NotFoundException("FormQuestion", request.QuestionId);

        var position = FractionalIndex.Create(request.Position ?? question.Position.Value);
        var config = FormQuestionConfig.FromConfig(question.QuestionType, request.ConfigJson);

        if (request.BoardFieldId is { } fieldId
            && !await (from f in _context.Forms.AsNoTracking()
                       join bf in _context.BoardFields.AsNoTracking() on f.BoardId equals bf.BoardId
                       where f.Id == request.FormId && bf.Id == fieldId && bf.DeletedAt == null
                       select bf.Id).AnyAsync(ct))
            throw new NotFoundException("BoardField", fieldId);

        form.UpdateQuestion(question, request.Label, request.IsRequired, position, config, request.BoardFieldId, _requestContext.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
