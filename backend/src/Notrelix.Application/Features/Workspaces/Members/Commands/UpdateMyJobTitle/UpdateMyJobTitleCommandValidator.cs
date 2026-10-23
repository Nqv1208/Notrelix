namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;

public class UpdateMyJobTitleCommandValidator : AbstractValidator<UpdateMyJobTitleCommand>
{
    public UpdateMyJobTitleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.JobTitle!)
            .Must(title => title.Trim().Length <= WorkspaceMember.MaxJobTitleLength)
            .WithMessage($"A job title is at most {WorkspaceMember.MaxJobTitleLength} characters.")
            .Must(title => !title.Any(char.IsControl))
            .WithMessage("A job title cannot contain control characters or line breaks.")
            .When(x => x.JobTitle is not null)
            .OverridePropertyName(nameof(UpdateMyJobTitleCommand.JobTitle));
    }
}
