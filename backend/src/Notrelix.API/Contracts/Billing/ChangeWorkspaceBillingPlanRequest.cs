namespace Notrelix.API.Contracts.Billing;

/// <summary>The catalog plan code to move the account to (free, pro or enterprise).</summary>
public sealed record ChangeWorkspaceBillingPlanRequest(string PlanCode);
