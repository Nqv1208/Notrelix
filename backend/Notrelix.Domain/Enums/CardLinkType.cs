namespace Notrelix.Domain.Enums;

// Loại liên kết giữa các card
public enum CardLinkType
{
    RelatesTo = 0,
    Blocks = 1,
    BlockedBy = 2,
    DuplicateOf = 3,
    ClonedFrom = 4
}
