using global::Notrelix.Application.Common.Models;
using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Cards.CreateCard;

public class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(v => v.ListId)
            .NotEmpty().WithMessage("ListId is required.");

        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
    }
}
