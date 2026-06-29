import { useQuery } from "@tanstack/react-query"
import { billingApi } from "../../api/billing.api"
import { getEntitlementValue, hasEntitlement } from "../model/entitlement-guards"
import type { EntitlementFeature } from "../model/entitlement.types"

interface EntitlementContext {
  workspaceId: string
}

export function useEntitlement(feature: EntitlementFeature, context: EntitlementContext) {
  const { data: subscription, isLoading } = useQuery({
    queryKey: ["billing", "subscription", context.workspaceId],
    queryFn: () => billingApi.getSubscription(context.workspaceId),
    enabled: Boolean(context.workspaceId),
    staleTime: 60_000,
  })

  const planId = subscription?.planId
  const value = getEntitlementValue(planId, feature)
  const isEnabled = hasEntitlement(planId, feature)

  return {
    value,
    isEnabled,
    isLoading,
  }
}
