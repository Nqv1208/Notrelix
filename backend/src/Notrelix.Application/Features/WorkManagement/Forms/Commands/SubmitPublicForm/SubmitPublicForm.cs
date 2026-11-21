using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Application.Features.WorkManagement.Forms.Services;
using ValidationException = global::Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitPublicForm;

/// <summary>
/// An anonymous visitor submits answers to a public form. The request carries no person and no permission: the
/// form's own principal (<see cref="PublicFormScope"/>) may insert submissions for that one form and nothing
/// else, so a visitor can never read, update or delete anything, and never reaches another board. The form
/// must be open to the public (published, public, live board, active workspace with public links on), else
/// 404 without saying which. A captcha the provider confirms is required; a filled honeypot is answered as a
/// success and stored nowhere. The answers are checked exactly like a member's, and the entry lands in the
/// response inbox as Accepted: an item is created only when a member processes it.
/// </summary>
public record SubmitPublicFormCommand(
    string PublicKey,
    Dictionary<string, JsonElement> Answers,
    string? CaptchaToken,
    string? Honeypot,
    string? RemoteIp)
    : ICommand<Result>, IWriteRequest, IAnonymousRequest, IGlobalRequest;

public class SubmitPublicFormCommandHandler : IRequestHandler<SubmitPublicFormCommand, Result>
{
    private readonly PublicFormScope _scope;
    private readonly ICaptchaVerifier _captcha;
    private readonly IWorkManagementDbContext _context;
    private readonly IDateTimeProvider _clock;

    public SubmitPublicFormCommandHandler(PublicFormScope scope, ICaptchaVerifier captcha, IWorkManagementDbContext context, IDateTimeProvider clock)
    {
        _scope = scope;
        _captcha = captcha;
        _context = context;
        _clock = clock;
    }

    public async Task<Result> Handle(SubmitPublicFormCommand request, CancellationToken ct)
    {
        var form = await _scope.OpenAsync(request.PublicKey, ct)
            ?? throw new NotFoundException("Form", request.PublicKey.Length > 4 ? request.PublicKey[..4] : "?");

        // Only a form that is open to the public spends a captcha check, so the endpoint is no oracle for keys.
        if (string.IsNullOrWhiteSpace(request.CaptchaToken)
            || !await _captcha.VerifyAsync(request.CaptchaToken, request.RemoteIp, ct))
            throw new ValidationException([new ValidationFailure("captchaToken", "Captcha verification failed.")]);

        // A filled honeypot is a bot: answer as a success so it learns nothing, and store nothing.
        if (!string.IsNullOrWhiteSpace(request.Honeypot)) return Result.Success();

        var options = (await _scope.MappedOptionsAsync(form, ct))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Select(o => o.Id).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var failures = FormAnswerValidator.Validate(form.Questions, request.Answers, options);
        if (failures.Count > 0) throw new ValidationException(failures);

        // No submitter, no address, no user agent: the visitor is anonymous and the form keeps nothing about them.
        _context.FormSubmissions.Add(FormSubmission.Create(
            form.AccountId, form.WorkspaceId, form.Id, form.BoardId, null,
            null, null, JsonSerializer.Serialize(request.Answers), null, null, _clock.UtcNow));
        return Result.Success();
    }
}

public class SubmitPublicFormCommandValidator : AbstractValidator<SubmitPublicFormCommand>
{
    public SubmitPublicFormCommandValidator()
    {
        RuleFor(x => x.PublicKey).NotEmpty().MaximumLength(64);
        RuleFor(x => x.CaptchaToken).MaximumLength(4096);
    }
}
