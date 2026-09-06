import type { Meta, StoryObj } from "@storybook/react";

import {
  searchResultsDefaultScenario,
  searchResultsEdgeDataScenario,
  searchResultsEmptyScenario,
} from "../verification/search-ui-fixtures";
import { SearchResultsSurface } from "./search-results-surface";

const meta: Meta<typeof SearchResultsSurface> = {
  title: "Search/Search Results",
  component: SearchResultsSurface,
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
  args: {
    query: "plan",
    results: searchResultsDefaultScenario(),
    activeTypes: ["page", "board", "task"],
  },
  tags: ["fui-surface--search.results", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    query: "",
    results: searchResultsEmptyScenario(),
    activeTypes: ["page", "board", "task", "block"],
  },
  tags: ["fui-surface--search.results", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    query: "enterprise",
    results: searchResultsEdgeDataScenario(),
    activeTypes: ["page", "board", "block"],
  },
  tags: ["fui-surface--search.results", "fui-state--EdgeData"],
};
