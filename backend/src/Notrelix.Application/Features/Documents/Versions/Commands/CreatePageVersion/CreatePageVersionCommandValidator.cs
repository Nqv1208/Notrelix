using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;

public class CreatePageVersionCommandValidator : AbstractValidator<CreatePageVersionCommand>
{
    public CreatePageVersionCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(DocumentVersion.MaxNameLength);
    }
}
