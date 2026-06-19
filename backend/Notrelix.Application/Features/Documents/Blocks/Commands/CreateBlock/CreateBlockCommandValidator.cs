using FluentValidation;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

public class CreateBlockCommandValidator : AbstractValidator<CreateBlockCommand>
{
    public CreateBlockCommandValidator()
    {
    }
}
