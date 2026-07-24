namespace Notrelix.Domain.Collaboration.Rules;

public static class AttachmentRules
{
    public static void EnsureMaxAttachments(int currentCount, int maxAllowed)
    {
        if (currentCount >= maxAllowed)
            throw new BusinessRuleException(BusinessRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded, $"Cannot exceed maximum of {maxAllowed} attachments per resource.");
    }

    public static void EnsureValidFileSize(long fileSizeBytes, long maxFileSizeBytes)
    {
        if (fileSizeBytes <= 0)
            throw new BusinessRuleException(BusinessRuleCodes.Collaboration_Attachment_FileSizeMustBePositive, "File size must be greater than zero.");

        if (fileSizeBytes > maxFileSizeBytes)
            throw new BusinessRuleException(BusinessRuleCodes.Collaboration_Attachment_FileSizeExceeded, $"File size exceeds maximum allowed size of {maxFileSizeBytes} bytes.");
    }
}
