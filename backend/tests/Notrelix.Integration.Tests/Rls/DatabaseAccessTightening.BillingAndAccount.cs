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
    public async Task AppRole_HasNoWriteOnCommercialBillingTables()
    {
        foreach (var table in new[]
                 {
                     "billing.billing_customers", "billing.payment_methods", "billing.invoices", "billing.invoice_line_items",
                     "billing.subscription_items", "billing.entitlements", "billing.billing_events", "billing.usage_metrics",
                     "billing.usage_metric_history", "billing.plans", "billing.plan_prices", "billing.plan_limits",
                 })
        {
            foreach (var privilege in new[] { "INSERT", "UPDATE", "DELETE" })
            {
                (await OwnerScalarAsync($"SELECT has_table_privilege('notrelix_app', '{table}', '{privilege}')"))
                    .Should().Be("False", $"{privilege} on {table}");
            }
        }

        // The usage ledger is append-only for the application role.
        (await OwnerScalarAsync("SELECT has_table_privilege('notrelix_app', 'billing.feature_usage_ledger', 'INSERT')")).Should().Be("True");
        (await OwnerScalarAsync("SELECT has_table_privilege('notrelix_app', 'billing.feature_usage_ledger', 'UPDATE')")).Should().Be("False");
        (await OwnerScalarAsync("SELECT has_table_privilege('notrelix_app', 'billing.feature_usage_ledger', 'DELETE')")).Should().Be("False");
    }

    [Fact]
    public async Task AccountAdmin_CanRenameTheAccount_ButNeverMoveItsPlanOrStatus()
    {
        await AsAppAsync(_a.User.Id, async (c, tx) =>
        {
            (await IsDeniedAsync(c, tx, $"UPDATE account.accounts SET plan_code = 'enterprise' WHERE id = '{_a.AccountId}'"))
                .Should().BeTrue("the plan belongs to the platform operator");
            (await IsDeniedAsync(c, tx, $"UPDATE account.accounts SET status = 'Suspended' WHERE id = '{_a.AccountId}'"))
                .Should().BeTrue("the account status belongs to the platform operator");
            (await IsDeniedAsync(c, tx, $"UPDATE account.accounts SET deleted_at = now() WHERE id = '{_a.AccountId}'"))
                .Should().BeTrue("deleting an account is not a tenant-side write");
            (await ExecAsync(c, tx, $"UPDATE account.accounts SET name = 'Renamed', updated_at = now() WHERE id = '{_a.AccountId}'"))
                .Should().Be(1, "renaming stays available to an account administrator");
        });
    }
}
