// Public API for the billing feature slice.
// Explicit exports only.

export type Plan = {
  id: string
  name: string
  priceMonthly: number
  priceYearly: number
  features: string[]
}

export type SubscriptionDetails = {
  planId: string
  status: "active" | "canceled" | "past_due" | "trialing"
  currentPeriodEnd: string
}

// Minimal correct contracts for billing entitlement guards
export const billingApi = {
  async getPlans(): Promise<Plan[]> {
    return [
      { id: "free", name: "Free", priceMonthly: 0, priceYearly: 0, features: ["Up to 3 boards", "Basic docs"] },
      { id: "pro", name: "Pro", priceMonthly: 12, priceYearly: 100, features: ["Unlimited boards", "Collaborative docs", "Automations"] },
      { id: "enterprise", name: "Enterprise", priceMonthly: 49, priceYearly: 480, features: ["Custom roles", "Audit logs", "SAML SSO"] }
    ]
  },

  async getSubscription(workspaceId: string): Promise<SubscriptionDetails> {
    return {
      planId: "free",
      status: "active",
      currentPeriodEnd: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString()
    }
  }
}
