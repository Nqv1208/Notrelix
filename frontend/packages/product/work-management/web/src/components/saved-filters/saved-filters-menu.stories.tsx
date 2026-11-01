import type { Meta, StoryObj } from "@storybook/react";
import {
  savedFiltersAppliedScenario,
  savedFiltersDefaultScenario,
  savedFiltersEmptyScenario,
  savedFiltersReadOnlyScenario,
  type SavedFiltersScenario,
} from "../../verification/saved-filters-fixtures";
import { SavedFiltersMenu } from "./saved-filters-menu";

const ok = async () => ({ ok: true }) as const;
const noop = () => undefined;
const callbacks = {
  onApply: noop,
  onClearApplied: noop,
  onRetry: noop,
  onSave: ok,
  onRename: ok,
  onSetVisibility: noop,
  onRemove: noop,
};

function Bar({ scenario }: { scenario: SavedFiltersScenario }) {
  return (
    <div className="flex flex-wrap items-center gap-1.5 px-4 py-2.5">
      <SavedFiltersMenu {...scenario} {...callbacks} />
    </div>
  );
}

const meta: Meta = {
  title: "Work Management/Saved Filters/Menu",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** A filter is on the bar: "Lưu bộ lọc" and the "Bộ lọc đã lưu" menu. */
export const Default: Story = {
  render: () => <Bar scenario={savedFiltersDefaultScenario()} />,
  tags: ["fui-surface--wm.saved-filters.menu", "fui-state--Default"],
};

/** A saved filter is applied: the menu button carries its name. */
export const Applied: Story = {
  render: () => <Bar scenario={savedFiltersAppliedScenario()} />,
};

/** A viewer cannot save: only the menu of saved filters. */
export const ReadOnly: Story = {
  render: () => <Bar scenario={savedFiltersReadOnlyScenario()} />,
  tags: ["fui-surface--wm.saved-filters.menu", "fui-state--ReadOnly"],
};

/** A board with no saved filters yet. */
export const EdgeData: Story = {
  render: () => <Bar scenario={savedFiltersEmptyScenario()} />,
  tags: ["fui-surface--wm.saved-filters.menu", "fui-state--EdgeData"],
};
