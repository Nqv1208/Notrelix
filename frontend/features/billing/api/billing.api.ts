import { isMockModeEnabled } from "@/lib/config/mock-mode"
import { mockBillingApi } from "./billing.mock"
import type { Plan, SubscriptionDetails } from "../model/billing.contract"

export const billingApi = {
  async getPlans(): Promise<Plan[]> {
    if (isMockModeEnabled("billing")) {
      return mockBillingApi.getPlans()
    }
    // Return empty list in production as it is not integrated
    return []
  },

  async getSubscription(workspaceId: string): Promise<SubscriptionDetails> {
    if (isMockModeEnabled("billing")) {
      return mockBillingApi.getSubscription(workspaceId)
    }
    throw new Error("Billing API is not integrated in production yet")
  }
}
