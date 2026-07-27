namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;

public class UpdateFieldOptionCommandValidator : AbstractValidator<UpdateFieldOptionCommand>
{
    public UpdateFieldOptionCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20);
    }
}
