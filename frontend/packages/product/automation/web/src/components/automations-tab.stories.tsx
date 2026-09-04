import type { Meta, StoryObj } from "@storybook/react";
import {
  automationRulesDefaultScenario,
  automationRulesEdgeDataScenario,
  automationRulesEmptyScenario,
} from "@notrelix/automation-testing";

import { AutomationsTab } from "./automations-tab";

const meta: Meta<typeof AutomationsTab> = {
  title: "Automation/Automations Tab",
  component: AutomationsTab,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-background p-6 text-foreground">
        <div className="mx-auto max-w-3xl">
          <Story />
        </div>
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    rules: automationRulesDefaultScenario(),
  },
  tags: ["fui-surface--automation.rules.tab", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    rules: automationRulesEmptyScenario(),
  },
  tags: ["fui-surface--automation.rules.tab", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    rules: automationRulesEdgeDataScenario(),
  },
  tags: ["fui-surface--automation.rules.tab", "fui-state--EdgeData"],
};
