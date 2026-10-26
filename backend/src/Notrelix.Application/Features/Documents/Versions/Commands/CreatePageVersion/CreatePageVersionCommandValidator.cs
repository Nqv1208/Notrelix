using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;

public class CreatePageVersionCommandValidator : AbstractValidator<CreatePageVersionCommand>
{
    public CreatePageVersionCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(DocumentVersion.MaxNameLength);
    }
}
