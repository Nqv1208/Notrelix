import type { Meta, StoryObj } from "@storybook/react";
import {
  statusOptionFormCreateScenario,
  statusOptionFormDuplicateScenario,
  statusOptionFormEdgeDataScenario,
  statusOptionFormEditScenario,
  statusOptionFormPendingScenario,
} from "../../verification/status-option-form-fixtures";
import { StatusOptionForm } from "./status-option-form";

const noop = () => undefined;
const callbacks = { onSubmit: noop, onCancel: noop };

const meta: Meta = {
  title: "Work Management/Status option/Form",
  parameters: { layout: "centered" },
  decorators: [
    (Story) => (
      <div className="w-72 rounded-md bg-popover p-4 shadow-popover ring-1 ring-foreground/10">
        <Story />
      </div>
    ),
  ],
};
export default meta;
type Story = StoryObj;

/** A new status: empty name, "Đang làm" group, its blue. */
export const Default: Story = {
  render: () => (
    <StatusOptionForm {...statusOptionFormCreateScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.status-option.form", "fui-state--Default"],
};

/** Changing an existing status: values filled in, its own name is not a duplicate. */
export const Edit: Story = {
  render: () => (
    <StatusOptionForm {...statusOptionFormEditScenario()} {...callbacks} />
  ),
};

/** The name is taken: the message shows and the form is not sent. */
export const Duplicate: Story = {
  render: () => (
    <StatusOptionForm {...statusOptionFormDuplicateScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.status-option.form", "fui-state--Error"],
};

/** The write is in flight. */
export const Pending: Story = {
  render: () => (
    <StatusOptionForm {...statusOptionFormPendingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.status-option.form", "fui-state--Loading"],
};

/** A very long name on a field without groups. */
export const EdgeData: Story = {
  render: () => (
    <StatusOptionForm {...statusOptionFormEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.status-option.form", "fui-state--EdgeData"],
};
