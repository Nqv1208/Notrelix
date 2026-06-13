namespace Notrelix.Infrastructure.Data;

public enum SeedProfile
{
    Small = 0,
    Medium = 1,
    Large = 2
}

public sealed class SeedDataOptions
{
    public bool Enabled { get; set; }
    public SeedProfile Profile { get; set; } = SeedProfile.Small;
    public bool ResetBeforeSeed { get; set; }

    public SeedTargets GetTargets() => SeedTargets.ForProfile(Profile);
}

public sealed record SeedTargets(
    int UserCount,
    int WorkspaceCount,
    int BoardCount,
    int BoardGroupCount,
    int BoardFieldCount,
    int BoardItemCount,
    int BoardViewCount,
    int LabelCount,
    int PageCount,
    int BlockCount,
    int CommentCount,
    int NotificationCount)
{
    public static SeedTargets ForProfile(SeedProfile profile) => profile switch
    {
        SeedProfile.Small => new(10, 5, 20, 80, 120, 400, 40, 60, 100, 500, 400, 30),
        SeedProfile.Medium => new(50, 10, 50, 200, 400, 2_000, 100, 150, 500, 2_500, 4_000, 250),
        SeedProfile.Large => new(200, 20, 200, 800, 1_600, 12_000, 400, 600, 2_000, 10_000, 24_000, 1_000),
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown seed profile.")
    };
}
