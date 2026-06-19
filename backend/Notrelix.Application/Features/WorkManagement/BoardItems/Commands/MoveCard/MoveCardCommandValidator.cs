using global::Notrelix.Application.Common.Models;
using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveCard;

public class MoveCardCommandValidator : AbstractValidator<MoveCardCommand>
{
    public MoveCardCommandValidator()
    {
        RuleFor(v => v.BoardItemId)
            .NotEmpty().WithMessage("BoardItemId is required.");

        RuleFor(v => v.GroupId)
            .NotEmpty().WithMessage("GroupId is required.");
            
        // Position có thể là số âm hoặc dương tùy thuật toán ở client, nên không có rule strict.
    }
}
