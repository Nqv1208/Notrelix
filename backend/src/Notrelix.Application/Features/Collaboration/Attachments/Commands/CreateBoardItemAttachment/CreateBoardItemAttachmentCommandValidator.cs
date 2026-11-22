using Notrelix.Application.Features.Collaboration.Attachments.Services;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

public class CreateBoardItemAttachmentCommandValidator : AbstractValidator<CreateBoardItemAttachmentCommand>
{
    public CreateBoardItemAttachmentCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.Filename)
            .Must(AttachmentFileName.IsValidLinkName)
            .WithMessage($"A link name must have 1 to {AttachmentFileName.MaxLinkNameLength} visible characters.");

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(AttachmentLinkUrl.MaxLength)
            .Must(AttachmentLinkUrl.IsAllowed)
            .WithMessage("A link must be an absolute http or https address.");
    }
}
