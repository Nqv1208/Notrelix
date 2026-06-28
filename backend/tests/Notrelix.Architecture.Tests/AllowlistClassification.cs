namespace Notrelix.Architecture.Tests;

/// <summary>
/// Classification for architecture test allowlist entries.
/// Every entry in an allowlist must have a classification, reason, and target state.
/// </summary>
public enum AllowlistClassification
{
    /// <summary>Entry is intentionally exempt — this is the correct long-term behavior.</summary>
    Intentional,

    /// <summary>Entry is a known gap from pre-hardening code. Must be fixed.</summary>
    LegacyGap,

    /// <summary>Test is incorrect — this is not actually a violation.</summary>
    FalsePositive,

    /// <summary>Entry is a system/bootstrap command that cannot follow normal rules.</summary>
    SystemCommand,

    /// <summary>Entry is a public/unauthenticated endpoint that intentionally skips markers.</summary>
    PublicCommand,

    /// <summary>Entry will be fixed as part of a migration that is not yet complete.</summary>
    MigrationPending,
}

/// <summary>
/// Structured allowlist entry with classification, reason, and target state.
/// </summary>
public sealed record AllowlistEntry(
    string RequestTypeName,
    AllowlistClassification Classification,
    string Reason,
    string TargetState);
