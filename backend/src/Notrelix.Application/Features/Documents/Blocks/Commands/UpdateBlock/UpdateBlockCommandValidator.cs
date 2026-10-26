namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

public class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    public UpdateBlockCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.BlockId)
            .NotEmpty();

        RuleFor(x => x.ExpectedVersion)
            .NotNull().GreaterThan(0)
            .WithMessage("expectedVersion is required and must be the version of the block you read.");
    }
}
