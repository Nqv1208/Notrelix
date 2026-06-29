// Public API for the billing feature slice.
// Explicit exports only.

export type { Plan, SubscriptionDetails } from "./model/billing.contract"
export { billingApi } from "./api/billing.api"

// Entitlements exports
export type { EntitlementFeature, EntitlementValue, WorkspaceEntitlements } from "./entitlements/model/entitlement.types"
export { getEntitlementValue, hasEntitlement } from "./entitlements/model/entitlement-guards"
export { useEntitlement } from "./entitlements/hooks/use-entitlement"
