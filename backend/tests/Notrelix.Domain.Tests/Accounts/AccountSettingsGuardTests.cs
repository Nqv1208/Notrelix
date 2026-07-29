using FluentAssertions;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountSettingsGuardTests
{
    [Fact]
    public void Accounts_ShouldNotContain_GenericAccountSettingsEntity()
    {
        var assembly = typeof(Account).Assembly;
        var types = assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Domain.Accounts") == true)
            .ToList();

        var violations = types
            .Where(t => t.Name.Contains("AccountSettings", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            "Generic AccountSettings entity must not exist in Domain. " +
            "Technical settings belong outside Domain. " +
            "Business settings require typed VOs/aggregate fields.");
    }
}
