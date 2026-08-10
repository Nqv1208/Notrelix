export type PlanTier = "free" | "pro" | "business" | "enterprise";

export interface BillingPlan {
  id: string;
  name: string;
  tier: PlanTier;
  priceMonthly: number;
  priceYearly: number;
  features: string[];
}

export interface Subscription {
  id: string;
  workspaceId: string;
  planId: string;
  status: "active" | "canceled" | "past_due" | "trialing";
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelAt?: string;
}

export interface Invoice {
  id: string;
  workspaceId: string;
  amount: number;
  currency: string;
  status: "paid" | "pending" | "failed";
  createdAt: string;
  pdfUrl?: string;
}

export interface Entitlement {
  id: string;
  workspaceId: string;
  feature: string;
  limit: number;
  used: number;
}
