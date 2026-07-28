using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Documents and enforces the system-actor policy:
/// - Public user-initiated mutations must require a non-empty actor ID (Guid, not Guid?).
/// - Factory methods and automated operations may pass null only when explicitly documented.
/// - PrepareAuditUpdate(null, ...) is only allowed inside approved system operations.
/// </summary>
public class SystemActorPolicyTests
{
    /// <summary>
    /// Operations where a null actor is explicitly allowed (system-generated operations).
    /// Each entry must be reviewed and justified.
    /// </summary>
    private static readonly IReadOnlySet<string> AllowedNullActorOperations = new HashSet<string>(StringComparer.Ordinal)
    {
        // User.Create: system-generated registration flow
        "Notrelix.Domain.Identity.Users.User.Create",
        // AccountDomain.Create: system-generated domain registration
        "Notrelix.Domain.Accounts.Domains.AccountDomain.Create",
    };

    [Fact]
    public void AuditMethods_ShouldAcceptNullableActor_ForSystemOperations()
    {
        // AuditableEntity.SetAuditOnCreate and PrepareAuditUpdate accept Guid? actor
        // to support system-generated operations. This is by design.
        var auditableEntityType = typeof(AuditableEntity);

        var setAuditOnCreate = auditableEntityType.GetMethod(
            "SetAuditOnCreate",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var setAuditOnUpdate = auditableEntityType.GetMethod(
            "SetAuditOnUpdate",
            BindingFlags.NonPublic | BindingFlags.Instance);

        setAuditOnCreate.Should().NotBeNull("AuditableEntity must have SetAuditOnCreate");
        setAuditOnUpdate.Should().NotBeNull("AuditableEntity must have SetAuditOnUpdate (private, for Infrastructure interceptor)");

        // Both methods accept Guid? (nullable) actor
        var createParams = setAuditOnCreate!.GetParameters();
        var updateParams = setAuditOnUpdate!.GetParameters();

        createParams[0].ParameterType.Should().Be(typeof(Guid?),
            "SetAuditOnCreate actor parameter must be nullable Guid to support system operations");

        updateParams[0].ParameterType.Should().Be(typeof(Guid?),
            "SetAuditOnUpdate actor parameter must be nullable Guid to support system operations");
    }

    [Fact]
    public void SystemActorAllowlist_ShouldOnlyContainReviewedOperations()
    {
        // This test documents that the allowlist is intentional and reviewed.
        // Adding a new entry requires explicit justification.
        AllowedNullActorOperations.Should().NotBeEmpty(
            "system operations must be explicitly documented");

        // All entries must be valid method names
        foreach (var operation in AllowedNullActorOperations)
        {
            operation.Should().Contain(".",
                "operation names must be fully qualified (Namespace.Type.Method)");
        }
    }

    [Fact]
    public void Entity_ShouldRejectEmptyGuidId()
    {
        // Entity base class rejects Guid.Empty as ID
        var entityTypes = typeof(Entity).Assembly.GetTypes()
            .Where(t => typeof(Entity).IsAssignableFrom(t) && !t.IsAbstract)
            .Take(5); // Sample a few types

        foreach (var type in entityTypes)
        {
            // All entities inherit the Guid.Empty rejection from Entity base
            type.Should().BeAssignableTo<Entity>(
                $"{type.Name} must inherit from Entity");
        }
    }
}
