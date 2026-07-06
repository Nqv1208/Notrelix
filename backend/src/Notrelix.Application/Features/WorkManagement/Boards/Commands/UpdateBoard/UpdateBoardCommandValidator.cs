namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

using Notrelix.Domain.WorkManagement.Boards;

public class UpdateBoardCommandValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Background)
            .MaximumLength(2000)
            .When(x => x.Background is not null);

        RuleFor(x => x.Visibility)
            .Must(v => v is null || Enum.IsDefined(typeof(BoardVisibility), v))
            .WithMessage("Visibility must be a valid board visibility value.");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0)
            .When(x => x.ExpectedVersion.HasValue);
    }
}
