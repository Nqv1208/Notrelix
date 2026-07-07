namespace Notrelix.Application.Common.Requests.Security;

/// <summary>
/// Marker for system-internal requests (background jobs, migrations, admin operations).
/// System internal is a modifier — it does not imply a specific scope.
/// </summary>
public interface ISystemInternalRequest : IUseCaseSecurityRequirement
{
}
