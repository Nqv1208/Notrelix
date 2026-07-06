namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

public class CreateBlockCommandValidator : AbstractValidator<CreateBlockCommand>
{
    public CreateBlockCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Properties)
            .NotEmpty();

        RuleFor(x => x.Position)
            .NotEmpty();
    }
}
