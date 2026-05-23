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
    int WorkspaceCount,
    int UserCount,
    int BoardCount,
    int CardCount,
    int PageCount)
{
    public static SeedTargets ForProfile(SeedProfile profile) => profile switch
    {
        SeedProfile.Small => new SeedTargets(5, 10, 20, 500, 100),
        SeedProfile.Medium => new SeedTargets(20, 100, 100, 10_000, 2_000),
        SeedProfile.Large => new SeedTargets(100, 1_000, 1_000, 100_000, 20_000),
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown seed profile.")
    };
}
