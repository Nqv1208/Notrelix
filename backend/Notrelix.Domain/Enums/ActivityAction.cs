namespace Notrelix.Domain.Enums;

// Loại hành động được ghi log
public enum ActivityAction
{
    // CRUD actions
    Create = 0,
    Update = 1,
    Delete = 2,
    Restore = 3,
    
    // Status actions
    Archive = 10,
    Unarchive = 11,
    Complete = 12,
    Uncomplete = 13,
    
    // Collaboration actions
    Share = 20,
    Unshare = 21,
    Comment = 22,
    
    // Member actions
    Join = 30,
    Leave = 31,
    InviteMember = 32,
    RemoveMember = 33,
    UpdateRole = 34,
    
    // Content actions
    Move = 40,
    Duplicate = 41
}
