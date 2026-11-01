using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Domain.Documents.Rules;

public static class PageRules
{
    /// <summary>The most live blocks one page holds (the page is read and written as one set).</summary>
    public const int MaxBlocksPerPage = 5000;

    public static void EnsureTitleNotTooLong(string title, int maxLength = 500)
    {
        Guard.MaxLength(title, maxLength);
    }

    public static void EnsureCanEdit(PageStatus status)
    {
        if (status == PageStatus.Archived)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotEditArchived, "Cannot edit an archived page.");
    }

    public static void EnsureBlockCountWithinLimit(int liveBlocks)
    {
        if (liveBlocks > MaxBlocksPerPage)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_Page_BlockLimitReached, $"A page holds at most {MaxBlocksPerPage} blocks.");
    }
}
