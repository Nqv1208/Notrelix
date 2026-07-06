namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

public class CreateBoardItemAttachmentCommandValidator : AbstractValidator<CreateBoardItemAttachmentCommand>
{
    public CreateBoardItemAttachmentCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.Filename)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
