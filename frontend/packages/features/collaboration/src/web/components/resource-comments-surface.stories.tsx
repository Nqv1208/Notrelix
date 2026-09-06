import type { Meta, StoryObj } from "@storybook/react";

import {
  resourceCommentsDefaultScenario,
  resourceCommentsEdgeDataScenario,
  resourceCommentsEmptyScenario,
} from "../../verification/collaboration-ui-fixtures";
import { ResourceCommentsSurface } from "./resource-comments-surface";

const meta: Meta<typeof ResourceCommentsSurface> = {
  title: "Collaboration/Resource Comments",
  component: ResourceCommentsSurface,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-background p-6 text-foreground">
        <div className="mx-auto max-w-xl">
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
    comments: resourceCommentsDefaultScenario(),
    currentUserId: "current-user",
  },
  tags: ["fui-surface--collaboration.comments", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    comments: resourceCommentsEmptyScenario(),
    currentUserId: "current-user",
  },
  tags: ["fui-surface--collaboration.comments", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    comments: resourceCommentsEdgeDataScenario(),
    currentUserId: "current-user",
  },
  tags: ["fui-surface--collaboration.comments", "fui-state--EdgeData"],
};
