namespace Notrelix.Domain.Identity;

// Trạng thái của User
public enum UserStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2,
    PendingVerification = 3
}
