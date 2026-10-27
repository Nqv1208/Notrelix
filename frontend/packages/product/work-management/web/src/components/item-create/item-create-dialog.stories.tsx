import type { Meta, StoryObj } from "@storybook/react";
import {
  itemCreateDefaultScenario,
  itemCreateEdgeDataScenario,
  itemCreateErrorScenario,
  itemCreatePendingScenario,
} from "../../verification/item-create-fixtures";
import { ItemCreateDialog } from "./item-create-dialog";

const noop = () => undefined;
const callbacks = { onOpenChange: noop, onSubmit: noop };

const meta: Meta = {
  title: "Work Management/Item create",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** The full create form: name empty, first group and status chosen. */
export const Default: Story = {
  render: () => (
    <ItemCreateDialog {...itemCreateDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.item-create.dialog", "fui-state--Default"],
};

/** The request failed: the message shows and the form keeps what was typed. */
export const Failed: Story = {
  render: () => (
    <ItemCreateDialog {...itemCreateErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.item-create.dialog", "fui-state--Error"],
};

/** The request is in flight: the form cannot be sent again. */
export const Pending: Story = {
  render: () => (
    <ItemCreateDialog {...itemCreatePendingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.item-create.dialog", "fui-state--Loading"],
};

/** Opened from a lane's quick add with a long title, long group name and every member. */
export const EdgeData: Story = {
  render: () => (
    <ItemCreateDialog {...itemCreateEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.item-create.dialog", "fui-state--EdgeData"],
};
