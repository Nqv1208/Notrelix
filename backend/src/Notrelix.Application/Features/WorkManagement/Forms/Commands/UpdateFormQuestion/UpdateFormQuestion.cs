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
    Guid? BoardFieldId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
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
        var question = await _context.FormQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.FormId == request.FormId, ct);
        if (question is null) throw new NotFoundException("FormQuestion", request.QuestionId);

        var position = FractionalIndex.Create(request.Position ?? question.Position.Value);
        var config = FormQuestionConfig.FromConfig(question.QuestionType, request.ConfigJson);

        if (request.BoardFieldId is { } fieldId
            && !await (from f in _context.Forms.AsNoTracking()
                       join bf in _context.BoardFields.AsNoTracking() on f.BoardId equals bf.BoardId
                       where f.Id == request.FormId && bf.Id == fieldId && bf.DeletedAt == null
                       select bf.Id).AnyAsync(ct))
            throw new NotFoundException("BoardField", fieldId);

        question.UpdateQuestion(request.Label, request.IsRequired, position, config, request.BoardFieldId);

        return Result.Success();
    }
}
