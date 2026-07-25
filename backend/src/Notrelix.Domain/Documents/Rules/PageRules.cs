using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Domain.Documents.Rules;

public static class PageRules
{
    public static void EnsureTitleNotTooLong(string title, int maxLength = 500)
    {
        Guard.MaxLength(title, maxLength);
    }

    public static void EnsureCanEdit(PageStatus status)
    {
        if (status == PageStatus.Archived)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotEditArchived, "Cannot edit an archived page.");
    }
}
