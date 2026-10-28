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
