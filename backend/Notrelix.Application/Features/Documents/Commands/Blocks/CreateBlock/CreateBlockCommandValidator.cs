using FluentValidation;

namespace Notrelix.Application.Features.Document.Commands.Blocks.CreateBlock;

public class CreateBlockCommandValidator : AbstractValidator<CreateBlockCommand>
{
    public CreateBlockCommandValidator()
    {
    }
}
