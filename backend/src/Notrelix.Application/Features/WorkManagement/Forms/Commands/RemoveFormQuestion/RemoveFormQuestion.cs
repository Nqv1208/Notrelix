using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.RemoveFormQuestion;

[IdempotencyOperation("work-management.forms.remove-form-question.v1")]
public record RemoveFormQuestionCommand(Guid FormId, Guid QuestionId, long? ExpectedVersion = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class RemoveFormQuestionCommandHandler : IRequestHandler<RemoveFormQuestionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveFormQuestionCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveFormQuestionCommand request, CancellationToken ct)
    {
        var form = await _context.Forms.Include(f => f.Questions).FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null || form.IsDeleted) throw new NotFoundException("Form", request.FormId);

        var question = form.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question is null) throw new NotFoundException("FormQuestion", request.QuestionId);

        form.RemoveQuestion(request.QuestionId, _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.FormQuestions.Remove(question);
        return Result.Success();
    }
}

public class RemoveFormQuestionCommandValidator : AbstractValidator<RemoveFormQuestionCommand>
{
    public RemoveFormQuestionCommandValidator()
    {
        RuleFor(x => x.FormId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
