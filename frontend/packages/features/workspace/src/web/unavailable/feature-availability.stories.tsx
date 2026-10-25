import type { Meta, StoryObj } from "@storybook/react";

import { BillingCapabilitiesSurface } from "../billing/billing-capabilities-surface";
import { vi as copy } from "./feature-availability.copy";
import { FeatureUnavailableSurface } from "./feature-unavailable-surface";

/** Pages whose backend is not available yet, and the billing capabilities page, content area only. */
const meta: Meta = {
  title: "Workspace/Feature availability",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

const noop = () => undefined;
const Page = ({ children }: { children: React.ReactNode }) => (
  <div className="w-296 bg-background p-7">{children}</div>
);

export const IntegrationsUnavailable: Story = {
  render: () => (
    <Page>
      <FeatureUnavailableSurface copy={copy} feature="integrations" />
    </Page>
  ),
  tags: ["fui-surface--workspace.feature-unavailable", "fui-state--Default"],
};

export const ReportsUnavailable: Story = {
  render: () => (
    <Page>
      <FeatureUnavailableSurface copy={copy} feature="reports" />
    </Page>
  ),
  tags: ["fui-surface--workspace.feature-unavailable", "fui-state--Default"],
};

const capabilities = (
  state: Parameters<typeof BillingCapabilitiesSurface>[0]["state"],
) => (
  <Page>
    <BillingCapabilitiesSurface copy={copy} state={state} onRetry={noop} />
  </Page>
);

export const CapabilitiesDefault: Story = {
  render: () =>
    capabilities({
      status: "ready",
      data: [
        {
          code: "AUTOMATION_RULE",
          isGranted: true,
          isUnlimited: false,
          limit: 50,
          used: 12,
          remaining: 38,
        },
      ],
    }),
  tags: ["fui-surface--workspace.billing-capabilities", "fui-state--Default"],
};
export const CapabilitiesLoading: Story = {
  render: () => capabilities({ status: "loading" }),
  tags: ["fui-surface--workspace.billing-capabilities", "fui-state--Loading"],
};
export const CapabilitiesError: Story = {
  render: () => capabilities({ status: "error" }),
  tags: ["fui-surface--workspace.billing-capabilities", "fui-state--Error"],
};
export const CapabilitiesForbidden: Story = {
  render: () => capabilities({ status: "forbidden" }),
  tags: [
    "fui-surface--workspace.billing-capabilities",
    "fui-state--PermissionLimited",
  ],
};
export const CapabilitiesEdgeData: Story = {
  render: () =>
    capabilities({
      status: "ready",
      data: [
        {
          code: "AUTOMATION_RULE",
          isGranted: true,
          isUnlimited: true,
          limit: null,
          used: 7,
          remaining: null,
        },
        {
          code: "SOMETHING_ELSE",
          isGranted: false,
          isUnlimited: false,
          limit: null,
          used: 0,
          remaining: null,
        },
      ],
    }),
  tags: ["fui-surface--workspace.billing-capabilities", "fui-state--EdgeData"],
};

const PLANS = [
  { code: "free", name: "Free" },
  { code: "pro", name: "Pro" },
  { code: "enterprise", name: "Enterprise" },
] as const;

export const CapabilitiesWithPlan: Story = {
  render: () => (
    <Page>
      <BillingCapabilitiesSurface
        copy={copy}
        onRetry={noop}
        plan={{
          currentCode: "pro",
          plans: PLANS,
          canChange: true,
          changing: false,
          failed: false,
        }}
        onChangePlan={noop}
        state={{
          status: "ready",
          data: [
            {
              code: "AUTOMATION_RULE",
              isGranted: true,
              isUnlimited: false,
              limit: 100,
              used: 12,
              remaining: 88,
            },
            {
              code: "MEMBER_SEAT",
              isGranted: true,
              isUnlimited: false,
              limit: 500,
              used: 86,
              remaining: 414,
            },
            {
              code: "GUEST_SEAT",
              isGranted: true,
              isUnlimited: false,
              limit: 200,
              used: 4,
              remaining: 196,
            },
            {
              code: "STORAGE_GB",
              isGranted: true,
              isUnlimited: false,
              limit: 100,
              used: 12.4,
              remaining: 88,
            },
          ],
        }}
      />
    </Page>
  ),
  tags: ["fui-surface--workspace.billing-capabilities", "fui-state--Default"],
};
