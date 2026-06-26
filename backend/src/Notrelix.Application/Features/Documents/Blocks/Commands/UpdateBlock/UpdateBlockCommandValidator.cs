using FluentValidation;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

public class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    public UpdateBlockCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty();
    }
}
