namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

public class CreateBlockCommandValidator : AbstractValidator<CreateBlockCommand>
{
    public CreateBlockCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .NotEmpty();

        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .When(x => x.Id.HasValue);
    }
}
