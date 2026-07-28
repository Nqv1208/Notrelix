namespace Notrelix.Application.Features.Accounts.Abstractions.Records;

/// <summary>
/// Persistence record for account settings.
/// This is not a Domain entity — settings validation belongs in Application.
/// Used by IAccountDbContext as the DbSet type for account settings.
/// </summary>
public class AccountSettingRecord
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string SettingKey { get; set; } = null!;
    public string SettingValue { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
