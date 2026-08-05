namespace Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;

public class CreateSavedFilterCommandValidator : AbstractValidator<CreateSavedFilterCommand>
{
    public CreateSavedFilterCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
