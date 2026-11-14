using Notrelix.Application.Features.Collaboration.Attachments.Services;

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
            .MaximumLength(AttachmentLinkUrl.MaxLength)
            .Must(AttachmentLinkUrl.IsAllowed)
            .WithMessage("A link must be an absolute http or https address.");
    }
}
