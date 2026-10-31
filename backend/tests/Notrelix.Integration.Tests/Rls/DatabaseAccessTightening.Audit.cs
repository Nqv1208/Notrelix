using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

public sealed partial class DatabaseAccessTighteningTests
{
    [Fact]
    public async Task AuditTables_AreAppendOnlyForTheRuntimeLogins()
    {
        foreach (var table in new[] { "audit.audit_logs", "audit.security_events" })
        {
            foreach (var role in new[] { "notrelix_app", "notrelix_auth", "notrelix_support_readonly" })
            {
                foreach (var privilege in new[] { "UPDATE", "DELETE", "TRUNCATE" })
                {
                    (await OwnerScalarAsync($"SELECT has_table_privilege('{role}', '{table}', '{privilege}')"))
                        .Should().Be("False", $"{role} {privilege} {table}");
                }
            }

            (await OwnerScalarAsync($"SELECT has_table_privilege('notrelix_app', '{table}', 'INSERT')"))
                .Should().Be("True", $"the application role still appends to {table}");
        }
    }
}
