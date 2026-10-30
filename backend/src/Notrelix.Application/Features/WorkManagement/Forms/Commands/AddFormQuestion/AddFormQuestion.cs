using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;

[IdempotencyOperation("work-management.forms.add-form-question.v1")]
public record AddFormQuestionCommand(
    Guid FormId,
    string QuestionKey,
    FormQuestionType QuestionType,
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

public class AddFormQuestionCommandHandler : IRequestHandler<AddFormQuestionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddFormQuestionCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddFormQuestionCommand request, CancellationToken ct)
    {
        var form = await _context.Forms.Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null) throw new NotFoundException("Form", request.FormId);

        if (request.BoardFieldId is { } fieldId
            && !await _context.BoardFields.AsNoTracking().AnyAsync(f => f.Id == fieldId && f.BoardId == form.BoardId && f.DeletedAt == null, ct))
            throw new NotFoundException("BoardField", fieldId);

        var accountId = _requestContext.RequireAccountId();
        var position = FractionalIndex.Create(request.Position ?? FractionalIndex.Initial().Value);
        var config = FormQuestionConfig.FromConfig(request.QuestionType, request.ConfigJson);

        var question = FormQuestion.Create(
            accountId,
            form.WorkspaceId,
            form.Id,
            request.BoardFieldId,
            request.QuestionKey,
            request.Label,
            request.QuestionType,
            request.IsRequired,
            position,
            config);

        form.AddQuestion(question, _requestContext.UserId, _dateTimeProvider.UtcNow);

        _context.FormQuestions.Add(question);
        return Result.Success();
    }
}
