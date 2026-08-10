import React from "react";
import { Button } from "@notrelix/ui-web";
import { Check, Zap, Building2, Crown } from "lucide-react";

export type BillingPlanTier = "free" | "pro" | "business";

export interface BillingPageProps {
  workspaceId: string;
  currentPlan: BillingPlanTier;
}

const PLANS = [
  {
    tier: "free" as const,
    name: "Free",
    price: 0,
    icon: Zap,
    features: ["Up to 5 members", "3 boards", "1 GB storage", "Basic views"],
  },
  {
    tier: "pro" as const,
    name: "Pro",
    price: 12,
    icon: Crown,
    features: [
      "Unlimited members",
      "Unlimited boards",
      "10 GB storage",
      "All views",
      "Priority support",
    ],
  },
  {
    tier: "business" as const,
    name: "Business",
    price: 29,
    icon: Building2,
    features: [
      "Everything in Pro",
      "100 GB storage",
      "Advanced permissions",
      "Audit log",
      "SAML SSO",
    ],
  },
];

export function BillingPage({
  workspaceId: _workspaceId,
  currentPlan,
}: BillingPageProps): React.ReactNode {
  return (
    <div className="p-8 max-w-4xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight mb-1">Billing</h1>
        <p className="text-sm text-muted-foreground">
          Manage your workspace subscription and billing.
        </p>
      </div>

      <div className="mb-8">
        <h2 className="font-semibold text-sm mb-3">Current Plan</h2>
        <div className="rounded-xl border border-primary/20 bg-primary/5 p-5">
          <div className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-xl bg-primary/10">
              <Crown className="size-5 text-primary" />
            </div>
            <div>
              <p className="font-semibold capitalize">{currentPlan}</p>
              <p className="text-sm text-muted-foreground">
                {currentPlan === "free"
                  ? "Free forever"
                  : `$${currentPlan === "pro" ? "12" : "29"}/month per member`}
              </p>
            </div>
          </div>
        </div>
      </div>

      <div>
        <h2 className="font-semibold text-sm mb-3">Upgrade Plan</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {PLANS.map((plan) => {
            const Icon = plan.icon;
            const isCurrent = plan.tier === currentPlan;
            return (
              <div
                key={plan.tier}
                className={`rounded-xl border p-5 flex flex-col ${
                  isCurrent ? "border-primary bg-primary/5" : "border-border"
                }`}
              >
                <div className="flex items-center gap-2 mb-3">
                  <Icon className="size-4 text-muted-foreground" />
                  <h3 className="font-semibold text-sm">{plan.name}</h3>
                </div>
                <p className="text-2xl font-bold mb-1">
                  ${plan.price}
                  <span className="text-sm font-normal text-muted-foreground">
                    /mo
                  </span>
                </p>
                <ul className="space-y-2 mt-4 mb-6 flex-1">
                  {plan.features.map((feature) => (
                    <li
                      key={feature}
                      className="flex items-start gap-2 text-sm text-muted-foreground"
                    >
                      <Check className="size-4 text-primary shrink-0 mt-0.5" />
                      <span>{feature}</span>
                    </li>
                  ))}
                </ul>
                {isCurrent ? (
                  <Button variant="outline" disabled className="w-full">
                    Current plan
                  </Button>
                ) : (
                  <Button variant="outline" className="w-full" disabled>
                    {plan.price >
                    (PLANS.find((p) => p.tier === currentPlan)?.price ?? 0)
                      ? "Upgrade"
                      : "Downgrade"}
                  </Button>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
