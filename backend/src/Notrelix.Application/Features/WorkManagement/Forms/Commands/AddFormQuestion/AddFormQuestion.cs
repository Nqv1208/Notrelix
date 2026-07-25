using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;

public record AddFormQuestionCommand(
    Guid FormId,
    string QuestionKey,
    FormQuestionType QuestionType,
    string Label,
    bool IsRequired,
    string? ConfigJson,
    string? Position,
    string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Form, FormId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"add-form-question:{FormId}:{QuestionKey}";
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
        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null) throw new NotFoundException("Form", request.FormId);

        var accountId = _requestContext.RequireAccountId();
        var position = FractionalIndex.Create(request.Position ?? FractionalIndex.Initial().Value);
        var config = FormQuestionConfig.FromConfig(request.QuestionType, request.ConfigJson);

        var question = FormQuestion.Create(
            accountId,
            form.WorkspaceId,
            form.Id,
            null,
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
