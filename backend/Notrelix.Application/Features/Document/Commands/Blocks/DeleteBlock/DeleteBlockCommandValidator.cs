using FluentValidation;

namespace Notrelix.Application.Features.Document.Commands.Blocks.DeleteBlock;

public class DeleteBlockCommandValidator : AbstractValidator<DeleteBlockCommand>
{
    public DeleteBlockCommandValidator()
    {
    }
}
