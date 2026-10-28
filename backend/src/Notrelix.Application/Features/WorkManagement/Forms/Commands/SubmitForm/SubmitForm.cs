using System.Text.Json;
using FluentValidation.Results;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;

/// <summary>
/// A signed-in member submits answers to a published form. The submission is recorded as an accepted
/// (new) inbox entry; the Item is created by processing it, through the ordinary create-item path
/// (WM-019), so form answers never become a second work store. Needs the right to create Items on the Board.
/// </summary>
[IdempotencyOperation("work-management.forms.submit-form.v1")]
public record SubmitFormCommand(
    Guid FormId,
    Dictionary<string, JsonElement> Answers)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public class SubmitFormCommandHandler : IRequestHandler<SubmitFormCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SubmitFormCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(SubmitFormCommand request, CancellationToken ct)
    {
        var form = await _context.Forms.Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Id == request.FormId, ct);
        if (form is null || form.IsDeleted) throw new NotFoundException("Form", request.FormId);

        form.EnsureAcceptsSubmissions();

        var mappedIds = form.Questions.Where(q => q.BoardFieldId.HasValue).Select(q => q.BoardFieldId!.Value).ToList();
        var optionIds = mappedIds.Count == 0
            ? new Dictionary<Guid, HashSet<string>>()
            : (await _context.BoardFields.AsNoTracking().Include(f => f.Options)
                .Where(f => mappedIds.Contains(f.Id)).ToListAsync(ct))
                .Where(f => f.Options.Count > 0)
                .ToDictionary(f => f.Id, f => f.Options.Select(o => o.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var failures = FormAnswerValidator.Validate(form.Questions, request.Answers, optionIds);
        if (failures.Count > 0) throw new global::Notrelix.Application.Common.Exceptions.ValidationException(failures);

        var submission = FormSubmission.Create(
            form.AccountId, form.WorkspaceId, form.Id, form.BoardId, null,
            _requestContext.UserId, null,
            JsonSerializer.Serialize(request.Answers),
            null, null, _dateTimeProvider.UtcNow);
        _context.FormSubmissions.Add(submission);
        return Result<Guid>.Success(submission.Id);
    }
}

/// <summary>Checks answers against the form's questions: unknown keys, required, and the shape of each value by question type.</summary>
public static class FormAnswerValidator
{
    public static List<ValidationFailure> Validate(
        IEnumerable<FormQuestion> questions,
        IReadOnlyDictionary<string, JsonElement> answers,
        IReadOnlyDictionary<Guid, HashSet<string>> optionIdsByField)
    {
        var failures = new List<ValidationFailure>();
        var byKey = questions.ToDictionary(q => q.QuestionKey, StringComparer.OrdinalIgnoreCase);

        foreach (var key in answers.Keys.Where(k => !byKey.ContainsKey(k)))
            failures.Add(new ValidationFailure(key, "Unknown question."));

        foreach (var q in byKey.Values)
        {
            answers.TryGetValue(q.QuestionKey, out var value);
            var blank = value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString()))
                || (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0);
            if (blank)
            {
                if (q.IsRequired || q.Config?.Required == true) failures.Add(new ValidationFailure(q.QuestionKey, "This question is required."));
                continue;
            }

            // A mapped question takes the board field's options; an unmapped choice question takes its own configured keys.
            var options = q.BoardFieldId is { } id
                ? (optionIdsByField.TryGetValue(id, out var o) ? o : null)
                : q.Config?.Options.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
            var error = Check(q, value, options);
            if (error is not null) failures.Add(new ValidationFailure(q.QuestionKey, error));
        }
        return failures;
    }

    private static string? Check(FormQuestion q, JsonElement v, HashSet<string>? options)
    {
        var c = q.Config;
        switch (q.QuestionType)
        {
            case FormQuestionType.ShortText:
            case FormQuestionType.LongText:
            case FormQuestionType.Email:
            case FormQuestionType.Phone:
                if (v.ValueKind != JsonValueKind.String) return "Expected text.";
                var s = v.GetString()!;
                if (c?.MaxLength is { } max && s.Length > max) return $"At most {max} characters.";
                if (q.QuestionType == FormQuestionType.Email && !s.Contains('@')) return "Expected an email address.";
                return null;
            case FormQuestionType.Number:
                if (v.ValueKind != JsonValueKind.Number || !v.TryGetDecimal(out var n)) return "Expected a number.";
                if (c?.MinValue is { } min && n < min) return $"At least {min}.";
                if (c?.MaxValue is { } mx && n > mx) return $"At most {mx}.";
                return null;
            case FormQuestionType.Date:
                return v.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(v.GetString(), out _) ? null : "Expected a date.";
            case FormQuestionType.Checkbox:
                return v.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "Expected true or false.";
            case FormQuestionType.Select:
                if (v.ValueKind != JsonValueKind.String) return "Expected one choice.";
                return options is null || options.Contains(v.GetString()!) ? null : "Not an available choice.";
            case FormQuestionType.MultiSelect:
                if (v.ValueKind != JsonValueKind.Array || v.EnumerateArray().Any(e => e.ValueKind != JsonValueKind.String)) return "Expected a list of choices.";
                return options is null || v.EnumerateArray().All(e => options.Contains(e.GetString()!)) ? null : "Not an available choice.";
            default:
                return "This question type cannot be answered yet.";
        }
    }
}
