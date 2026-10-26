using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Templates.Commands.CreatePageFromTemplate;

public class CreatePageFromTemplateCommandValidator : AbstractValidator<CreatePageFromTemplateCommand>
{
    public CreatePageFromTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500);
    }
}
