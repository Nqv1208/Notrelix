using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Forms.Queries.GetForm;

/// <summary>A form with its ordered questions: what the builder edits and the member submit page renders.</summary>
public record GetFormQuery(Guid FormId) : IQuery<Result<FormDetailDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public partial class GetFormQueryHandler : IRequestHandler<GetFormQuery, Result<FormDetailDto>>
{
    private readonly IWorkManagementDbContext _context;

    public GetFormQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<FormDetailDto>> Handle(GetFormQuery request, CancellationToken ct)
    {
        var form = await _context.Forms.AsNoTracking().Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Id == request.FormId && f.DeletedAt == null, ct);
        if (form is null) throw new NotFoundException("Form", request.FormId);

        var questions = form.Questions.OrderBy(q => q.Position.Value, StringComparer.Ordinal)
            .Select(q => new FormQuestionDto(q.Id, q.QuestionKey, q.Label, q.QuestionType.ToString(), q.IsRequired, q.BoardFieldId, q.Position.Value, q.Config?.Placeholder, q.Config?.HelpText, q.Config?.MaxLength, q.Config?.MinValue, q.Config?.MaxValue, q.Config?.Options.Select(x => new FormQuestionOptionDto(x.Key, x.Label)).ToList()))
            .ToList();
        return Result<FormDetailDto>.Success(new FormDetailDto(form.Id, form.Name, form.Slug, form.BoardId, form.Status.ToString(), form.Visibility.ToString(), form.Version, form.CreatedAt, questions, ReadDescription(form.SettingsJson)));
    }
}

public partial class GetFormQueryHandler
{
    private static string? ReadDescription(string settingsJson)
    {
        try
        {
            return System.Text.Json.Nodes.JsonNode.Parse(settingsJson)?["description"]?.GetValue<string>();
        }
        catch (Exception)
        {
            return null;
        }
    }
}

public class GetFormQueryValidator : AbstractValidator<GetFormQuery>
{
    public GetFormQueryValidator() => RuleFor(x => x.FormId).NotEmpty();
}
