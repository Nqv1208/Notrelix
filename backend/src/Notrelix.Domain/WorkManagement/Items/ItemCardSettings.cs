namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>
/// The cover of an Item: one of ten colour tokens or an image attachment of the Item, shown as a strip on
/// top of the card or over the whole card. Work Management owns the choice; the attachment itself belongs to
/// Collaboration, so only its id is kept and Application checks it is an image of this Item.
/// </summary>
public static class ItemCover
{
    public const string StyleStrip = "strip";
    public const string StyleFull = "full";

    public static readonly IReadOnlyList<string> Colors =
        ["blue", "teal", "violet", "amber", "rose", "gray", "orange", "green", "indigo", "red"];

    public static bool IsColor(string? value) => value is not null && Colors.Contains(value);
    public static bool IsStyle(string? value) => value is StyleStrip or StyleFull;
}

/// <summary>"Lặp lại": completing a repeating Item reopens it with the next due date.</summary>
public static class ItemRepeat
{
    public const string Daily = "daily";
    public const string Weekdays = "weekdays";
    public const string Weekly = "weekly";
    public const string Biweekly = "biweekly";
    public const string Monthly = "monthly";

    public static readonly IReadOnlyList<string> Rules = [Daily, Weekdays, Weekly, Biweekly, Monthly];

    public static bool IsRule(string? value) => value is not null && Rules.Contains(value);

    /// <summary>The first occurrence of the rule after <paramref name="due"/> that is also after <paramref name="after"/>.</summary>
    public static DateTimeOffset NextDue(DateTimeOffset due, string rule, DateTimeOffset after)
    {
        var next = due;
        do
        {
            next = rule switch
            {
                Daily => next.AddDays(1),
                Weekdays => next.DayOfWeek switch
                {
                    DayOfWeek.Friday => next.AddDays(3),
                    DayOfWeek.Saturday => next.AddDays(2),
                    _ => next.AddDays(1),
                },
                Weekly => next.AddDays(7),
                Biweekly => next.AddDays(14),
                Monthly => next.AddMonths(1),
                _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown repeat rule."),
            };
        }
        while (next <= after);
        return next;
    }
}

/// <summary>
/// "Nhắc trước hạn": minutes before the due time at which assignees and followers are told. The default
/// (one day, stored as null) is the daily due-date job; <see cref="None"/> turns the reminder off.
/// </summary>
public static class ItemReminder
{
    public const int None = -1;
    public const int DefaultMinutes = 1440;

    public static readonly IReadOnlyList<int> Offsets = [None, 0, 15, 60, DefaultMinutes, 2880, 10080];

    public static bool IsOffset(int value) => Offsets.Contains(value);
}
