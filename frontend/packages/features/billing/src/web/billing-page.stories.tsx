import type { Meta, StoryObj } from "@storybook/react";

import {
  billingPageBusinessScenario,
  billingPageDefaultScenario,
  billingPageFreeScenario,
} from "../verification/billing-ui-fixtures";
import { BillingPage } from "./billing-page";

const meta: Meta<typeof BillingPage> = {
  title: "Billing/Billing Page",
  component: BillingPage,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-background text-foreground">
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: billingPageDefaultScenario(),
  tags: ["fui-surface--billing.page", "fui-state--Default"],
};

export const Empty: Story = {
  args: billingPageFreeScenario(),
  tags: ["fui-surface--billing.page", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: billingPageBusinessScenario(),
  tags: ["fui-surface--billing.page", "fui-state--EdgeData"],
};
