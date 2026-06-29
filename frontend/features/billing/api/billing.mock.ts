import type { Plan, SubscriptionDetails } from "../model/billing.contract"

export const mockBillingApi = {
  async getPlans(): Promise<Plan[]> {
    return [
      { id: "free", name: "Free", priceMonthly: 0, priceYearly: 0, features: ["Up to 3 boards", "Basic docs"] },
      { id: "pro", name: "Pro", priceMonthly: 12, priceYearly: 100, features: ["Unlimited boards", "Collaborative docs", "Automations"] },
      { id: "enterprise", name: "Enterprise", priceMonthly: 49, priceYearly: 480, features: ["Custom roles", "Audit logs", "SAML SSO"] }
    ]
  },

  async getSubscription(workspaceId: string): Promise<SubscriptionDetails> {
    void workspaceId
    return {
      planId: "free",
      status: "active",
      currentPeriodEnd: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString()
    }
  }
}
