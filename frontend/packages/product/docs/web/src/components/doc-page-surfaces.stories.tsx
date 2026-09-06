import type { Meta, StoryObj } from "@storybook/react";

import {
  docsBlocksDefaultScenario,
  docsBlocksEdgeDataScenario,
  docsBlocksEmptyScenario,
  docsBreadcrumbDefaultScenario,
  docsBreadcrumbEdgeDataScenario,
  docsCommentsDefaultScenario,
  docsCommentsEdgeDataScenario,
  docsCommentsEmptyScenario,
  docsHistoryDefaultScenario,
  docsHistoryEdgeDataScenario,
  docsHistoryEmptyScenario,
  docsPageScreenDefaultScenario,
  docsPageScreenEdgeDataScenario,
  docsPageScreenEmptyScenario,
  docsPageScreenLoadingScenario,
  docsPageTreeDefaultScenario,
  docsPageTreeEdgeDataScenario,
  docsPageTreeEmptyScenario,
} from "../verification/docs-ui-fixtures";
import {
  DocCommentsSurface,
  DocHistorySurface,
  DocPageHeaderSurface,
  DocPageScreenSurface,
  DocPageTreeSurface,
} from "./doc-page-surfaces";

const meta: Meta<typeof DocPageScreenSurface> = {
  title: "Docs/Page Surfaces",
  component: DocPageScreenSurface,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-muted/30 p-6 text-foreground">
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj;

export const PageScreenDefault: Story = {
  render: () => <DocPageScreenSurface {...docsPageScreenDefaultScenario()} />,
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenEmpty: Story = {
  render: () => <DocPageScreenSurface {...docsPageScreenEmptyScenario()} />,
  tags: ["fui-surface--docs.page.screen", "fui-state--Empty"],
};

export const PageScreenEdgeData: Story = {
  render: () => <DocPageScreenSurface {...docsPageScreenEdgeDataScenario()} />,
  tags: ["fui-surface--docs.page.screen", "fui-state--EdgeData"],
};

export const PageScreenLoading: Story = {
  render: () => <DocPageScreenSurface {...docsPageScreenLoadingScenario()} />,
  tags: ["fui-surface--docs.page.screen", "fui-state--Loading"],
};

export const PageTreeDefault: Story = {
  render: () => (
    <div className="h-[520px] w-[280px] overflow-hidden rounded-lg border bg-background">
      <DocPageTreeSurface
        pages={docsPageTreeDefaultScenario()}
        workspaceId="workspace-docs"
        currentPageId="page-operating-plan"
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.tree", "fui-state--Default"],
};

export const PageTreeEmpty: Story = {
  render: () => (
    <div className="h-[520px] w-[280px] overflow-hidden rounded-lg border bg-background">
      <DocPageTreeSurface
        pages={docsPageTreeEmptyScenario()}
        workspaceId="workspace-docs"
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.tree", "fui-state--Empty"],
};

export const PageTreeEdgeData: Story = {
  render: () => (
    <div className="h-[520px] w-[320px] overflow-hidden rounded-lg border bg-background">
      <DocPageTreeSurface
        pages={docsPageTreeEdgeDataScenario()}
        workspaceId="workspace-enterprise"
        currentPageId="page-enterprise-rollout"
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.tree", "fui-state--EdgeData"],
};

export const HeaderDefault: Story = {
  render: () => (
    <div className="w-[720px] overflow-hidden rounded-lg border bg-background">
      <DocPageHeaderSurface
        pageTitle="Operating plan"
        breadcrumbs={docsBreadcrumbDefaultScenario()}
        isFavorited
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.header", "fui-state--Default"],
};

export const HeaderEdgeData: Story = {
  render: () => (
    <div className="w-[760px] overflow-hidden rounded-lg border bg-background">
      <DocPageHeaderSurface
        pageTitle="Enterprise rollout readiness checklist with regional localization and audit evidence"
        breadcrumbs={docsBreadcrumbEdgeDataScenario()}
        isFavorited={false}
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.header", "fui-state--EdgeData"],
};

export const CommentsDefault: Story = {
  render: () => (
    <div className="w-[420px] rounded-lg border bg-background p-5">
      <DocCommentsSurface comments={docsCommentsDefaultScenario()} />
    </div>
  ),
  tags: ["fui-surface--docs.comments", "fui-state--Default"],
};

export const CommentsEmpty: Story = {
  render: () => (
    <div className="w-[420px] rounded-lg border bg-background p-5">
      <DocCommentsSurface comments={docsCommentsEmptyScenario()} />
    </div>
  ),
  tags: ["fui-surface--docs.comments", "fui-state--Empty"],
};

export const CommentsEdgeData: Story = {
  render: () => (
    <div className="w-[460px] rounded-lg border bg-background p-5">
      <DocCommentsSurface comments={docsCommentsEdgeDataScenario()} />
    </div>
  ),
  tags: ["fui-surface--docs.comments", "fui-state--EdgeData"],
};

export const HistoryDefault: Story = {
  render: () => (
    <div className="w-[420px] rounded-lg border bg-background p-5">
      <DocHistorySurface history={docsHistoryDefaultScenario()} />
    </div>
  ),
  tags: ["fui-surface--docs.history", "fui-state--Default"],
};

export const HistoryEmpty: Story = {
  render: () => (
    <div className="w-[420px] rounded-lg border bg-background p-5">
      <DocHistorySurface history={docsHistoryEmptyScenario()} />
    </div>
  ),
  tags: ["fui-surface--docs.history", "fui-state--Empty"],
};

export const HistoryEdgeData: Story = {
  render: () => (
    <div className="w-[460px] rounded-lg border bg-background p-5">
      <DocHistorySurface history={docsHistoryEdgeDataScenario()} />
    </div>
  ),
  tags: ["fui-surface--docs.history", "fui-state--EdgeData"],
};

export const BlocksDefault: Story = {
  render: () => (
    <div className="w-[640px] rounded-lg border bg-background p-6">
      <DocPageScreenSurface
        {...docsPageScreenDefaultScenario()}
        pages={docsPageTreeDefaultScenario()}
        blocks={docsBlocksDefaultScenario()}
      />
    </div>
  ),
  parameters: { chromatic: { disableSnapshot: true } },
};

export const BlocksEmpty: Story = {
  render: () => (
    <div className="w-[640px] rounded-lg border bg-background p-6">
      <DocPageScreenSurface
        {...docsPageScreenEmptyScenario()}
        blocks={docsBlocksEmptyScenario()}
      />
    </div>
  ),
  parameters: { chromatic: { disableSnapshot: true } },
};

export const BlocksEdgeData: Story = {
  render: () => (
    <div className="w-[640px] rounded-lg border bg-background p-6">
      <DocPageScreenSurface
        {...docsPageScreenEdgeDataScenario()}
        blocks={docsBlocksEdgeDataScenario()}
      />
    </div>
  ),
  parameters: { chromatic: { disableSnapshot: true } },
};
