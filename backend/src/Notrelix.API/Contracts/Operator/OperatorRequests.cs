namespace Notrelix.API.Contracts.Operator;

public sealed record OperatorSignInRequest(string? Email, string? Password);

public sealed record OperatorReasonRequest(string? Reason, long ExpectedVersion);

public sealed record OperatorChangePlanRequest(Guid PlanId, string? Reason, long? ExpectedVersion);

public sealed record OperatorAccountPlansRequest(IReadOnlyList<Guid> AccountIds);

public sealed record OperatorPlanLimitRequest(string FeatureCode, bool IsUnlimited, int Limit);

public sealed record OperatorPlanRevisionRequest(
    string FamilyKey, int Revision, string Name, string Tier, decimal PriceAmount, string Currency, string Period,
    IReadOnlyList<OperatorPlanLimitRequest>? Limits, string? Reason);

public sealed record OperatorGrantOverrideRequest(
    string FeatureCode, bool IsUnlimited, int Limit, DateTimeOffset? ExpiresAt, string? Reason);
