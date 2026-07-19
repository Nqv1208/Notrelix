using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;

public record UpdateFormQuestionCommand(
    Guid QuestionId,
    string Label,
    bool IsRequired,
    string? ConfigJson,
    string? Position,
    string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Form, QuestionId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-form-question:{QuestionId}";
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
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);
        if (question is null) throw new NotFoundException("FormQuestion", request.QuestionId);

        var position = FractionalIndex.Create(request.Position ?? question.Position.Value);
        var config = FormQuestionConfig.FromConfig(question.QuestionType, request.ConfigJson);

        question.UpdateQuestion(request.Label, request.IsRequired, position, config);

        return Result.Success();
    }
}
