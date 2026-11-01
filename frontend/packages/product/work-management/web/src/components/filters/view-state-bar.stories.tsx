import type { ReactNode } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  erpViewBar,
  type ViewBarOptions,
} from "../../verification/view-state-fixtures";

const owner = (values: string[]) => ({
  fieldId: "field-erp-owner",
  operator: "has_any" as const,
  values,
});

function Bar({ children }: { children: ReactNode }) {
  return (
    <div className="flex flex-wrap items-center gap-1.5 bg-background px-6 py-3">
      {children}
    </div>
  );
}

const render = (options: ViewBarOptions) => () => (
  <Bar>{erpViewBar(options)}</Bar>
);

const meta: Meta = {
  title: "Work Management/Filters/View bar",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** A personal view: a person and an overdue filter, a two-level sort and the group by the person. */
export const Default: Story = {
  render: render({
    view: "table",
    state: {
      filter: {
        join: "and",
        conditions: [
          owner(["user-dang-minh-tuan"]),
          { fieldId: "$overdue", operator: "is_true", values: [] },
        ],
      },
      sorts: [
        { fieldId: "field-erp-due", direction: "asc" },
        { fieldId: "field-erp-priority", direction: "desc" },
      ],
      group: { fieldId: "field-erp-due", bucket: "week" },
    },
    counts: { shown: 4, total: 16 },
  }),
  tags: ["fui-surface--wm.filters.bar", "fui-state--Default"],
};

/** Five conditions joined by OR (three chips and "+2"), a stale condition and a swimlane field. */
export const EdgeData: Story = {
  render: render({
    view: "kanban",
    state: {
      filter: {
        join: "or",
        conditions: [
          owner([
            "user-dang-minh-tuan",
            "user-phan-ngoc-anh",
            "user-khanh-linh",
          ]),
          {
            fieldId: "field-erp-priority",
            operator: "has_any",
            values: ["p-urgent", "p-high", "p-medium", "p-low"],
          },
          {
            fieldId: "$title",
            operator: "contains",
            values: ["đối soát hoá đơn đầu vào quý 3 chi nhánh phía Bắc"],
          },
          { fieldId: "$overdue", operator: "is_true", values: [] },
          { fieldId: "field-gone", operator: "equals", values: ["x"] },
        ],
      },
      lane: { fieldId: "field-erp-owner" },
    },
    counts: { shown: 9, total: 16 },
  }),
  tags: ["fui-surface--wm.filters.bar", "fui-state--EdgeData"],
};

/** A viewer filters for themselves; saving a filter is off with its reason. */
export const ReadOnly: Story = {
  render: render({
    view: "table",
    canSave: false,
    state: {
      filter: { join: "and", conditions: [owner(["user-phan-ngoc-anh"])] },
    },
    counts: { shown: 5, total: 16 },
  }),
  tags: ["fui-surface--wm.filters.bar", "fui-state--ReadOnly"],
};

/** Calendar: the filter alone, no sort, group or lane. */
export const Calendar: Story = {
  render: render({ view: "calendar" }),
};
