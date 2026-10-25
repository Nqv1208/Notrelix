namespace Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;

public class DeleteBlockCommandValidator : AbstractValidator<DeleteBlockCommand>
{
    public DeleteBlockCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.BlockId)
            .NotEmpty();
    }
}
