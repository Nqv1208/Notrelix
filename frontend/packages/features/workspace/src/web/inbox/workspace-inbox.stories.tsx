import type { ReactNode } from "react";
import type { Meta, StoryObj } from "@storybook/react";

import {
  inboxAllScenario,
  inboxDefaultScenario,
  inboxEdgeDataScenario,
  inboxEmptyScenario,
  inboxErrorScenario,
  inboxHighDensityScenario,
  inboxLoadingScenario,
  inboxProducedOnlyScenario,
} from "../../verification/workspace-inbox-fixtures";
import { WorkspaceInboxSurface } from "./workspace-inbox-surface";

const meta: Meta = {
  title: "Workspace/Inbox",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

const callbacks = {
  onMarkRead: noop,
  onMarkAllRead: noop,
  onOpenSettings: noop,
  onRetry: noop,
};

/** The page is as tall as the window; the story gives it the mockup's 900 px. */
function Stage({ children }: { children: ReactNode }) {
  return <div className="h-[900px] w-full">{children}</div>;
}

function renderInbox(scenario: ReturnType<typeof inboxDefaultScenario>) {
  return (
    <Stage>
      <WorkspaceInboxSurface {...scenario} {...callbacks} />
    </Stage>
  );
}

/** Final-Inbox `binh-thuong`: the need tab, unread only, the approval open. */
export const InboxDefault: Story = {
  render: () => renderInbox(inboxDefaultScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--Default"],
};

/** Final-Inbox `tat-ca`: every notification, read ones and the bundle included. */
export const InboxAll: Story = {
  render: () => renderInbox(inboxAllScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--Default"],
};

/** Final-Inbox `het-viec`. */
export const InboxEmpty: Story = {
  render: () => renderInbox(inboxEmptyScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--Empty"],
};

export const InboxLoading: Story = {
  render: () => renderInbox(inboxLoadingScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--Loading"],
};

export const InboxError: Story = {
  render: () => renderInbox(inboxErrorScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--Error"],
};

export const InboxEdgeData: Story = {
  render: () => renderInbox(inboxEdgeDataScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--EdgeData"],
};

export const InboxHighDensity: Story = {
  render: () => renderInbox(inboxHighDensityScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--HighDensity"],
};

/** What the host fills today: titles and messages, no type, person or context. */
export const InboxProducedOnly: Story = {
  render: () => renderInbox(inboxProducedOnlyScenario()),
  tags: ["fui-surface--workspace.inbox", "fui-state--EdgeData"],
};
