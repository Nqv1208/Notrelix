namespace Notrelix.Application.Common.Requests;

/// <summary>
/// Required expected-version rule of the P3 versioned-request manifest
/// (WM-V3-CONC-001, SPEC WMREQ110–114): the version is a required positive value
/// obtained from an authoritative read. A missing value binds as 0, which is never a
/// persisted version, so it is rejected as a client error instead of surfacing as a
/// guaranteed 412.
/// </summary>
public static class ExpectedVersionValidationExtensions
{
    public static IRuleBuilderOptions<T, long> MustBeAnAuthoritativeVersion<T>(this IRuleBuilder<T, long> rule) =>
        rule.GreaterThan(0).WithMessage("ExpectedVersion is required and must be the Version from an authoritative read.");
}
