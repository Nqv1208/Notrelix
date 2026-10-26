using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;

public class RestorePageVersionCommandValidator : AbstractValidator<RestorePageVersionCommand>
{
    public RestorePageVersionCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.CurrentBlocks).NotNull().Must(blocks => blocks.Count <= 20_000).WithMessage("Too many blocks.");
    }
}
