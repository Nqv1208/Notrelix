using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;

public class GetFullBoardQueryValidator : AbstractValidator<GetFullBoardQuery>
{
    public GetFullBoardQueryValidator()
    {
        RuleFor(v => v.BoardId)
            .NotEmpty().WithMessage("BoardId is required.");
    }
}
