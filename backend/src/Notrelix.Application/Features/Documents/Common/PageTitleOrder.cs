using System.Globalization;
using System.Text;

namespace Notrelix.Application.Features.Documents.Common;

/// <summary>
/// The order pages sort in: by title, case and marks ignored (so "Ân" sits with "An" and "đ" with "d"), the
/// same way people compare names. Sorting happens on this key in memory over a bounded read, so it does not
/// depend on the database collation.
/// </summary>
internal static class PageTitleOrder
{
    public static string Key(string title)
    {
        var decomposed = title.Normalize(NormalizationForm.FormD);
        var key = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            key.Append(ch switch { 'đ' => 'd', 'Đ' => 'D', _ => ch });
        }

        return key.ToString().ToLowerInvariant();
    }
}
