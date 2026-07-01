namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

using Notrelix.Domain.WorkManagement.Boards;

public class CreateBoardInWorkspaceCommandValidator : AbstractValidator<CreateBoardInWorkspaceCommand>
{
    public CreateBoardInWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Background)
            .MaximumLength(2000);

        RuleFor(x => x.Visibility)
            .Must(v => v is null || Enum.IsDefined(typeof(BoardVisibility), v))
            .WithMessage("Visibility must be a valid board visibility value.");
    }
}
