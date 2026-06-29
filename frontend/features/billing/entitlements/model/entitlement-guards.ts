import type { EntitlementFeature, EntitlementValue } from "./entitlement.types"

const PLAN_ENTITLEMENTS: Record<string, Record<EntitlementFeature, EntitlementValue>> = {
  free: {
    "boards.limit": 3,
    "docs.collaboration": false,
    "automation": false,
    "governance.custom-roles": false,
    "governance.audit-logs": false,
  },
  pro: {
    "boards.limit": Infinity,
    "docs.collaboration": true,
    "automation": true,
    "governance.custom-roles": false,
    "governance.audit-logs": false,
  },
  enterprise: {
    "boards.limit": Infinity,
    "docs.collaboration": true,
    "automation": true,
    "governance.custom-roles": true,
    "governance.audit-logs": true,
  },
}

export function getEntitlementValue(planId: string | undefined, feature: EntitlementFeature): EntitlementValue {
  const plan = planId?.toLowerCase().trim() || "free"
  const entitlements = PLAN_ENTITLEMENTS[plan] || PLAN_ENTITLEMENTS.free
  return entitlements[feature]
}

export function hasEntitlement(planId: string | undefined, feature: EntitlementFeature): boolean {
  const value = getEntitlementValue(planId, feature)
  if (typeof value === "boolean") {
    return value
  }
  return true // Numeric limits are considered enabled but restricted by counts
}
