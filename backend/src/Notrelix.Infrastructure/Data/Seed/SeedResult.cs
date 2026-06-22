namespace Notrelix.Infrastructure.Data.Seed;

internal sealed record SeedResult
{
    public int UsersCreated { get; init; }
    public int WorkspacesCreated { get; init; }
    public int BoardsCreated { get; init; }
    public int BoardItemsCreated { get; init; }
    public int PagesCreated { get; init; }
    public int CommentsCreated { get; init; }
    public int NotificationsCreated { get; init; }
    public bool Skipped { get; init; }
}
