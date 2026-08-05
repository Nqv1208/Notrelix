namespace Notrelix.Application.Features.WorkManagement.Views.Commands.RestoreSavedFilter;

public class RestoreSavedFilterCommandValidator : AbstractValidator<RestoreSavedFilterCommand>
{
    public RestoreSavedFilterCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
    }
}
