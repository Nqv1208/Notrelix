namespace Notrelix.Application.Features.WorkManagement.Views.Commands.DeleteSavedFilter;

public class DeleteSavedFilterCommandValidator : AbstractValidator<DeleteSavedFilterCommand>
{
    public DeleteSavedFilterCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
