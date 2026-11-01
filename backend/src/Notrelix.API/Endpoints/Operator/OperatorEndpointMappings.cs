using Notrelix.API.Contracts.Operator;
using Notrelix.API.RateLimiting;
using Notrelix.API.Security;
using Notrelix.Application.Features.Accounts.Operator;
using Notrelix.Application.Features.Billing.Operator;
using Notrelix.Application.Features.Operators.Audit;
using Notrelix.Application.Features.Operators.Auth;
using Notrelix.Application.Features.Operators.Public.Principal;
using Notrelix.Infrastructure.Operators;

namespace Notrelix.API.Endpoints.Operator;

/// <summary>
/// The platform operator console API. Operators sign in with a password and receive a token no tenant route
/// accepts; every other route here refuses a tenant token. The operator never reads tenant content: the account
/// list shows identity and status, the detail adds two counts, and the audit trail shows what operators changed.
/// </summary>
public static class OperatorEndpointMappings
{
    public static IEndpointRouteBuilder MapOperatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPublicPost("/operator/v1/auth/sign-in", SignInAsync)
            .WithMetadata(new OperatorEndpointAttribute(), new RateLimitPolicyAttribute("OperatorSignInByIp"))
            .WithName("Operator.Auth.SignIn")
            .WithTags("Operator")
            .WithSummary("Sign a platform operator in with a password")
            .Produces<OperatorSessionDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var group = app.MapGroup("/operator/v1").WithTags("Operator").AddEndpointFilter<OperatorSessionFilter>();

        group.MapOperatorGet("/auth/me", (ICurrentOperator current) =>
                Results.Ok(new OperatorProfileDto(current.OperatorId, current.Email, current.DisplayName)))
            .WithName("Operator.Auth.Me").WithSummary("The signed-in operator")
            .Produces<OperatorProfileDto>();

        // ── Accounts ────────────────────────────────────────
        group.MapOperatorGet("/accounts", async (string? search, int? skip, int? take, OperatorAccountService accounts, CancellationToken ct) =>
                Results.Ok(await accounts.ListAsync(search, skip ?? 0, take ?? 25, ct)))
            .WithName("Operator.Account.List").WithSummary("List accounts (identity and status only)")
            .Produces<OperatorAccountPageDto>();

        group.MapOperatorGet("/accounts/{accountId:guid}", async (Guid accountId, OperatorAccountService accounts, CancellationToken ct) =>
                Results.Ok(await accounts.GetAsync(accountId, ct)))
            .WithName("Operator.Account.Get").WithSummary("One account with its member and workspace counts")
            .Produces<OperatorAccountDetailDto>();

        group.MapOperatorPost("/accounts/{accountId:guid}/suspend", async (Guid accountId, OperatorReasonRequest request, OperatorAccountService accounts, CancellationToken ct) =>
                Results.Ok(await accounts.SuspendAsync(accountId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Account.Suspend").WithSummary("Suspend an account: read-only for its members, nothing deleted")
            .Produces<OperatorAccountDto>();

        group.MapOperatorPost("/accounts/{accountId:guid}/lift-suspension", async (Guid accountId, OperatorReasonRequest request, OperatorAccountService accounts, CancellationToken ct) =>
                Results.Ok(await accounts.LiftSuspensionAsync(accountId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Account.LiftSuspension").WithSummary("Lift an account's suspension")
            .Produces<OperatorAccountDto>();

        // ── Billing of one account ──────────────────────────
        group.MapOperatorPost("/billing/account-plans", async (OperatorAccountPlansRequest request, OperatorAccountBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GetPlansAsync(request.AccountIds ?? [], ct)))
            .WithName("Operator.Billing.AccountPlans").WithSummary("The plan each listed account is on")
            .Produces<IReadOnlyList<OperatorAccountPlanDto>>();

        group.MapOperatorGet("/accounts/{accountId:guid}/billing", async (Guid accountId, OperatorAccountBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GetAsync(accountId, ct)))
            .WithName("Operator.Billing.Get").WithSummary("An account's plan, subscription and entitlement overrides")
            .Produces<OperatorAccountBillingDto>();

        group.MapOperatorPut("/accounts/{accountId:guid}/billing/plan", async (Guid accountId, OperatorChangePlanRequest request, OperatorAccountBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.ChangePlanAsync(accountId, request.PlanId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Billing.ChangePlan").WithSummary("Move an account to a catalog plan revision")
            .Produces<OperatorAccountBillingDto>();

        group.MapOperatorPost("/accounts/{accountId:guid}/billing/terminate", async (Guid accountId, OperatorReasonRequest request, OperatorAccountBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.TerminateSubscriptionAsync(accountId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Billing.Terminate").WithSummary("End an account's subscription now")
            .Produces<OperatorAccountBillingDto>();

        group.MapOperatorPost("/accounts/{accountId:guid}/billing/overrides", async (Guid accountId, OperatorGrantOverrideRequest request, OperatorAccountBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.GrantOverrideAsync(
                    accountId, new OperatorOverrideInput(request.FeatureCode, request.IsUnlimited, request.Limit, request.ExpiresAt, request.Reason), ct)))
            .WithName("Operator.Billing.GrantOverride").WithSummary("Grant a manual entitlement override")
            .Produces<OperatorAccountBillingDto>();

        group.MapOperatorPost("/billing/overrides/{entitlementId:guid}/revoke", async (Guid entitlementId, OperatorReasonRequest request, OperatorAccountBillingService billing, CancellationToken ct) =>
                Results.Ok(await billing.RevokeOverrideAsync(entitlementId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Billing.RevokeOverride").WithSummary("Revoke a manual entitlement override")
            .Produces<OperatorAccountBillingDto>();

        // ── Plan catalog ────────────────────────────────────
        group.MapOperatorGet("/billing/plans", async (OperatorPlanCatalogService plans, CancellationToken ct) =>
                Results.Ok(await plans.ListAsync(ct)))
            .WithName("Operator.Billing.Plans.List").WithSummary("Every revision of every plan family")
            .Produces<IReadOnlyList<OperatorPlanDto>>();

        group.MapOperatorPost("/billing/plans", async (OperatorPlanRevisionRequest request, OperatorPlanCatalogService plans, CancellationToken ct) =>
                Results.Ok(await plans.CreateRevisionAsync(new OperatorPlanRevisionInput(
                    request.FamilyKey, request.Revision, request.Name, request.Tier, request.PriceAmount, request.Currency, request.Period,
                    (request.Limits ?? []).Select(l => new OperatorPlanLimitDto(l.FeatureCode, l.IsUnlimited, l.Limit)).ToList(),
                    request.Reason), ct)))
            .WithName("Operator.Billing.Plans.CreateRevision").WithSummary("Publish a new plan revision")
            .Produces<OperatorPlanDto>();

        group.MapOperatorPost("/billing/plans/{planId:guid}/deprecate", async (Guid planId, OperatorReasonRequest request, OperatorPlanCatalogService plans, CancellationToken ct) =>
                Results.Ok(await plans.DeprecateAsync(planId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Billing.Plans.Deprecate").WithSummary("Stop selling a plan revision")
            .Produces<OperatorPlanDto>();

        group.MapOperatorPost("/billing/plans/{planId:guid}/archive", async (Guid planId, OperatorReasonRequest request, OperatorPlanCatalogService plans, CancellationToken ct) =>
                Results.Ok(await plans.ArchiveAsync(planId, request.Reason, request.ExpectedVersion, ct)))
            .WithName("Operator.Billing.Plans.Archive").WithSummary("Archive a plan revision")
            .Produces<OperatorPlanDto>();

        // ── Audit ───────────────────────────────────────────
        group.MapOperatorGet("/audit", async (Guid? accountId, int? take, OperatorAuditReader audit, CancellationToken ct) =>
                Results.Ok(await audit.ListAsync(accountId, take ?? 50, ct)))
            .WithName("Operator.Audit.List").WithSummary("What operators changed, newest first")
            .Produces<IReadOnlyList<OperatorAuditEntryDto>>();

        return app;
    }

    private static async Task<IResult> SignInAsync(OperatorSignInRequest request, OperatorSignInService signIn, CancellationToken ct) =>
        Results.Ok(await signIn.SignInAsync(request.Email, request.Password, ct));
}
