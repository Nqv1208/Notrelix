
using System.Reflection;

namespace Notrelix.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    private static readonly MethodInfo SetAuditOnCreateMethod =
        typeof(AuditableEntity).GetMethod("SetAuditOnCreate", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo SetAuditOnUpdateMethod =
        typeof(AuditableEntity).GetMethod("SetAuditOnUpdate", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public AuditableEntityInterceptor(ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = _dateTimeProvider.UtcNow;
        Guid? userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetAuditOnCreateMethod.Invoke(entry.Entity, [userId, now]);
                    break;
                case EntityState.Modified:
                    SetAuditOnUpdateMethod.Invoke(entry.Entity, [userId, now]);
                    break;
            }
        }
    }
}
