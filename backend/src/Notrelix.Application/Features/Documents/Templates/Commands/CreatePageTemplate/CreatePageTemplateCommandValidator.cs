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

namespace Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;

public class CreatePageTemplateCommandValidator : AbstractValidator<CreatePageTemplateCommand>
{
    public CreatePageTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(1024);
    }
}
