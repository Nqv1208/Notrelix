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
