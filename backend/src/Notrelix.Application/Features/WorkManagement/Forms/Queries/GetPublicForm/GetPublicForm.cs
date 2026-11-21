using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.WorkManagement.Forms.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Forms.Queries.GetPublicForm;

/// <summary>The form an anonymous visitor fills: only what they need to answer it, never a board or field id.</summary>
public record PublicFormDto(
    string Name,
    string? Description,
    IReadOnlyList<PublicFormQuestionDto> Questions,
    string? CaptchaSiteKey);

public record PublicFormQuestionDto(
    string Key,
    string Label,
    string Type,
    bool Required,
    string? Placeholder,
    string? HelpText,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    IReadOnlyList<FormQuestionOptionDto>? Options);

public record GetPublicFormQuery(string PublicKey) : IQuery<Result<PublicFormDto>>, IReadRequest, IAnonymousRequest, IGlobalRequest;

public class GetPublicFormQueryHandler : IRequestHandler<GetPublicFormQuery, Result<PublicFormDto>>
{
    private readonly PublicFormScope _scope;
    private readonly ICaptchaVerifier _captcha;

    public GetPublicFormQueryHandler(PublicFormScope scope, ICaptchaVerifier captcha)
    {
        _scope = scope;
        _captcha = captcha;
    }

    public async Task<Result<PublicFormDto>> Handle(GetPublicFormQuery request, CancellationToken ct)
    {
        var form = await _scope.OpenAsync(request.PublicKey, ct)
            ?? throw new NotFoundException("Form", request.PublicKey.Length > 4 ? request.PublicKey[..4] : "?");
        var mapped = await _scope.MappedOptionsAsync(form, ct);

        var questions = form.Questions
            .Where(q => FormQuestionRules.IsSupported(q.QuestionType))
            .OrderBy(q => q.Position.Value, StringComparer.Ordinal)
            .Select(q => new PublicFormQuestionDto(
                q.QuestionKey, q.Label, q.QuestionType.ToString(), q.IsRequired || q.Config?.Required == true,
                q.Config?.Placeholder, q.Config?.HelpText, q.Config?.MaxLength, q.Config?.MinValue, q.Config?.MaxValue,
                q.BoardFieldId is { } field && mapped.TryGetValue(field, out var o)
                    ? o.Select(x => new FormQuestionOptionDto(x.Id, x.Name)).ToList()
                    : q.Config?.Options.Select(x => new FormQuestionOptionDto(x.Key, x.Label)).ToList()))
            .ToList();
        return Result<PublicFormDto>.Success(new PublicFormDto(form.Name, ReadDescription(form.SettingsJson), questions, _captcha.SiteKey));
    }

    private static string? ReadDescription(string settingsJson)
    {
        try { return System.Text.Json.Nodes.JsonNode.Parse(settingsJson)?["description"]?.GetValue<string>(); }
        catch (Exception) { return null; }
    }
}
