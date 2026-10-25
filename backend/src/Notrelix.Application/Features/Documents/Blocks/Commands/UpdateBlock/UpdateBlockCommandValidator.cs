namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

public class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    public UpdateBlockCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.BlockId)
            .NotEmpty();
    }
}
