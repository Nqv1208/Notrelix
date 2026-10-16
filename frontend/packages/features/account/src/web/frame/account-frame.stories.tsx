import type { Meta, StoryObj } from "@storybook/react";

import {
  frameDefaultScenario,
  frameEdgeDataScenario,
} from "../../verification/account-frame-fixtures";
import { AccountFrame } from "./account-frame";

const meta: Meta = {
  title: "Account/Frame",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const page = <p className="m-0 text-body text-muted-foreground">…</p>;

export const Default: Story = {
  render: () => <AccountFrame {...frameDefaultScenario()}>{page}</AccountFrame>,
  tags: ["fui-surface--account.frame", "fui-state--Default"],
};

export const EdgeData: Story = {
  render: () => (
    <AccountFrame {...frameEdgeDataScenario()}>{page}</AccountFrame>
  ),
  tags: ["fui-surface--account.frame", "fui-state--EdgeData"],
};
