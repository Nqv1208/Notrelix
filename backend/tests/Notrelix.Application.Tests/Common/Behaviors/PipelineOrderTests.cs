using System.Text.RegularExpressions;
using FluentAssertions;

namespace Notrelix.Application.Tests.Common.Behaviors;

/// <summary>
/// Freezes the MediatR pipeline behavior registration order.
/// The execution order of pipeline behaviors is a stable contract:
/// reordering behaviors without updating this test is a breaking change.
/// </summary>
public sealed class PipelineOrderTests
{
    private static readonly string[] ExpectedBehaviorOrder =
    [
        // Outer zone: pre-DB
        "ExceptionMappingBehavior",
        "ApplicationTracingBehavior",
        "ValidationBehavior",
        "RequestContractGuardBehavior",
        "TokenValidationBehavior",
        "TenantBootstrapBehavior",
        "SystemOperationAuditBehavior",
        "ResourceScopeBehavior",
        // Post-commit scope wraps DB scope
        "PostCommitScopeBehavior",
        // Public cache before DB scope
        "PublicCacheBehavior",
        // DB/RLS/Transaction boundary
        "DbRequestScopeBehavior",
        // Inner zone: inside DB/RLS scope
        "AuthorizationBehavior",
        "VerifiedEmailBehavior",
        "ConcurrencyBehavior",
        "SubscriptionGateBehavior",
        "FeatureGateBehavior",
        "IdempotencyBehavior",
        "PostCommitEnqueueBehavior",
        "AuthorizedCacheBehavior",
    ];

    [Fact]
    public void Pipeline_behaviors_must_be_registered_in_frozen_order()
    {
        var sourcePath = FindDependencyInjectionPath();
        var source = File.ReadAllText(sourcePath);

        var pattern = @"typeof\(IPipelineBehavior<,>\),\s*typeof\((\w+)<,>\)";
        var matches = Regex.Matches(source, pattern);

        var registeredNames = matches
            .Select(m => m.Groups[1].Value)
            .ToArray();

        registeredNames.Should().Equal(
            ExpectedBehaviorOrder,
            "pipeline behavior registration order is a frozen execution contract. " +
            "Reordering behaviors changes authorization, transaction, and cache semantics. " +
            $"Registered: [{string.Join(", ", registeredNames)}]");
    }

    [Fact]
    public void Pipeline_must_have_exactly_19_behaviors()
    {
        var sourcePath = FindDependencyInjectionPath();
        var source = File.ReadAllText(sourcePath);

        var pattern = @"typeof\(IPipelineBehavior<,>\),\s*typeof\((\w+)<,>\)";
        var matches = Regex.Matches(source, pattern);

        matches.Count.Should().Be(19,
            "adding or removing a pipeline behavior requires explicit architecture review");
    }

    [Fact]
    public void DbRequestScope_must_be_after_contract_guard_and_before_authorization()
    {
        var dbIndex = Array.IndexOf(ExpectedBehaviorOrder, "DbRequestScopeBehavior");
        var guardIndex = Array.IndexOf(ExpectedBehaviorOrder, "RequestContractGuardBehavior");
        var authIndex = Array.IndexOf(ExpectedBehaviorOrder, "AuthorizationBehavior");

        dbIndex.Should().BeGreaterThan(guardIndex,
            "contract guard must reject invalid markers before opening a DB scope");
        authIndex.Should().BeGreaterThan(dbIndex,
            "authorization must run inside the DB/RLS scope to query tenant-scoped permissions");
    }

    [Fact]
    public void Idempotency_must_be_inside_db_scope()
    {
        var dbIndex = Array.IndexOf(ExpectedBehaviorOrder, "DbRequestScopeBehavior");
        var idempotencyIndex = Array.IndexOf(ExpectedBehaviorOrder, "IdempotencyBehavior");

        idempotencyIndex.Should().BeGreaterThan(dbIndex,
            "idempotency must run inside the transaction to atomically complete with business state");
    }

    [Fact]
    public void PostCommitScope_must_wrap_db_scope()
    {
        var postCommitIndex = Array.IndexOf(ExpectedBehaviorOrder, "PostCommitScopeBehavior");
        var dbIndex = Array.IndexOf(ExpectedBehaviorOrder, "DbRequestScopeBehavior");

        postCommitIndex.Should().BeLessThan(dbIndex,
            "post-commit scope must wrap the DB scope so side effects flush after transaction commit");
    }

    private static string FindDependencyInjectionPath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName, "src", "Notrelix.Application", "DependencyInjection.cs");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Cannot locate src/Notrelix.Application/DependencyInjection.cs from test working directory.");
    }
}
