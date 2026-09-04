import type { BillingPageProps } from "../web/billing-page";

export function billingPageDefaultScenario(): BillingPageProps {
  return {
    workspaceId: "ws-main",
    currentPlan: "pro",
  };
}

export function billingPageFreeScenario(): BillingPageProps {
  return {
    workspaceId: "ws-personal",
    currentPlan: "free",
  };
}

export function billingPageBusinessScenario(): BillingPageProps {
  return {
    workspaceId: "ws-enterprise",
    currentPlan: "business",
  };
}
