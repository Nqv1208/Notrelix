namespace Notrelix.Application.Common.Requests.Security;

/// <summary>
/// A request whose one use case needs more than one permission action on the
/// same resource (for example a Workspace invitation naming both Team and Space
/// entries needs ManageTeams and ManageSpaces). <see cref="IRequirePermission.Action"/>
/// stays the primary action; every action named here must also be allowed by the
/// engine, each evaluated over its own facts and permission rules, before the
/// handler runs. The access-control behavior owns the check, never the handler.
/// Actions are the names of the Governance action vocabulary, so this marker
/// carries only technical strings.
/// </summary>
public interface IRequireAdditionalPermission : IRequirePermission
{
    IReadOnlyList<string> AdditionalActionNames { get; }
}
