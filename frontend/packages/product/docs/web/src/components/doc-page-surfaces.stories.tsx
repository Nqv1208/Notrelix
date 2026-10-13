import type { Meta, StoryObj } from "@storybook/react";

import {
  BLOCKS,
  COMMENTS,
  HEADER,
  RELATED_DOCS,
  SHARE,
  TASKS,
  TOC,
  VERSIONS,
  docsCopy,
  docsPageScreenCommentsScenario,
  docsPageScreenContentsScenario,
  docsPageScreenDefaultScenario,
  docsPageScreenEmptyScenario,
  docsPageScreenFileMenuScenario,
  docsPageScreenLoadingScenario,
  docsPageScreenMentionScenario,
  docsPageScreenNotFoundScenario,
  docsPageScreenReadOnlyScenario,
  docsPageScreenSaveErrorScenario,
  docsPageScreenShareScenario,
  docsPageScreenSlashScenario,
  docsPageScreenVersionsScenario,
} from "../verification/docs-ui-fixtures";
import {
  DocArticleSurface,
  DocPageBarSurface,
  DocPageScreenSurface,
  DocShareDialogSurface,
  DocSidePanelSurface,
  DocToolbarSurface,
  DocVersionBarSurface,
  DocVersionPanelSurface,
} from "./doc-page-surfaces";

const meta: Meta<typeof DocPageScreenSurface> = {
  title: "Docs/Page Surfaces",
  component: DocPageScreenSurface,
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

/** The main area of the workspace shell: a full-height column. */
function Frame({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex h-[900px] w-full max-w-[1108px] flex-col overflow-hidden bg-doc-page text-foreground">
      {children}
    </div>
  );
}

const ARTICLE = {
  copy: docsCopy,
  title: HEADER.title,
  chips: docsPageScreenDefaultScenario().chips,
  why: docsPageScreenDefaultScenario().why,
  blocks: BLOCKS,
  overlay: { kind: "none" } as const,
  mentionTasks: [],
};

export const PageScreenDefault: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenDefaultScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenLoading: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenLoadingScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Loading"],
};

export const PageScreenNotFound: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenNotFoundScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Unavailable"],
};

export const PageScreenEmpty: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenEmptyScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Empty"],
};

export const PageScreenReadOnly: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenReadOnlyScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--ReadOnly"],
};

export const PageScreenSaveError: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenSaveErrorScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Error"],
};

export const PageScreenComments: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenCommentsScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenContents: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenContentsScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenFileMenu: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenFileMenuScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenSlashMenu: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenSlashScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenMentionMenu: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenMentionScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenShare: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenShareScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const PageScreenVersionHistory: Story = {
  render: () => (
    <Frame>
      <DocPageScreenSurface {...docsPageScreenVersionsScenario()} />
    </Frame>
  ),
  tags: ["fui-surface--docs.page.screen", "fui-state--Default"],
};

export const BarDefault: Story = {
  render: () => (
    <div className="w-[1108px] bg-doc-page">
      <DocPageBarSurface
        copy={docsCopy}
        header={HEADER}
        saveState="saved"
        canShare
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.bar", "fui-state--Default"],
};

export const BarSaveError: Story = {
  render: () => (
    <div className="w-[1108px] bg-doc-page">
      <DocPageBarSurface
        copy={docsCopy}
        header={HEADER}
        saveState="error"
        canShare
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.bar", "fui-state--Error"],
};

export const ToolbarDefault: Story = {
  render: () => (
    <div className="w-[1108px] bg-doc-page">
      <DocToolbarSurface
        copy={docsCopy}
        fileMenuOpen={false}
        canFormat
        current={{
          blockType: "heading_2",
          fontSize: 20,
          bold: true,
          italic: false,
          underline: false,
          strike: false,
          align: "left",
        }}
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.toolbar", "fui-state--Default"],
};

export const ToolbarFileMenu: Story = {
  render: () => (
    <div className="h-[460px] w-[1108px] bg-doc-page">
      <DocToolbarSurface
        copy={docsCopy}
        fileMenuOpen
        downloadOpen
        canShare
        canFormat
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.toolbar", "fui-state--Default"],
};

export const ArticleDefault: Story = {
  render: () => (
    <div className="flex h-[900px] w-[799px] justify-center overflow-auto bg-doc-page px-8 pt-9 pb-16">
      <DocArticleSurface {...ARTICLE} />
    </div>
  ),
  tags: ["fui-surface--docs.page.article", "fui-state--Default"],
};

export const ArticleEmpty: Story = {
  render: () => (
    <div className="flex h-[400px] w-[799px] justify-center bg-doc-page px-8 pt-9">
      <DocArticleSurface
        {...ARTICLE}
        title="Tài liệu chưa đặt tên"
        chips={[]}
        why={null}
        blocks={[]}
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.article", "fui-state--Empty"],
};

export const ArticleSlashMenu: Story = {
  render: () => {
    const scenario = docsPageScreenSlashScenario();
    return (
      <div className="flex h-[900px] w-[799px] justify-center overflow-auto bg-doc-page px-8 pt-9 pb-16">
        <DocArticleSurface
          {...ARTICLE}
          blocks={scenario.blocks}
          overlay={scenario.overlay}
        />
      </div>
    );
  },
  tags: ["fui-surface--docs.page.article", "fui-state--Default"],
};

export const ArticleMentionMenu: Story = {
  render: () => {
    const scenario = docsPageScreenMentionScenario();
    return (
      <div className="flex h-[900px] w-[799px] justify-center overflow-auto bg-doc-page px-8 pt-9 pb-16">
        <DocArticleSurface
          {...ARTICLE}
          blocks={scenario.blocks}
          overlay={scenario.overlay}
          mentionTasks={scenario.mentionTasks}
        />
      </div>
    );
  },
  tags: ["fui-surface--docs.page.article", "fui-state--Default"],
};

export const ArticleVersionCompare: Story = {
  render: () => {
    const scenario = docsPageScreenVersionsScenario();
    return (
      <div className="flex h-[900px] w-[799px] justify-center overflow-auto bg-doc-page px-8 pt-9 pb-16">
        <DocArticleSurface
          {...ARTICLE}
          blocks={scenario.blocks}
          readOnly
          compare
        />
      </div>
    );
  },
  tags: ["fui-surface--docs.page.article", "fui-state--Default"],
};

export const SidePanelLinks: Story = {
  render: () => (
    <div className="flex h-[900px]">
      <DocSidePanelSurface
        copy={docsCopy}
        tab="links"
        tasks={TASKS}
        relatedDocs={RELATED_DOCS}
        toc={TOC}
        comments={COMMENTS}
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.side-panel", "fui-state--Default"],
};

export const SidePanelContents: Story = {
  render: () => (
    <div className="flex h-[900px]">
      <DocSidePanelSurface
        copy={docsCopy}
        tab="toc"
        tasks={TASKS}
        relatedDocs={RELATED_DOCS}
        toc={TOC}
        comments={COMMENTS}
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.side-panel", "fui-state--Default"],
};

export const SidePanelComments: Story = {
  render: () => (
    <div className="flex h-[900px]">
      <DocSidePanelSurface
        copy={docsCopy}
        tab="comments"
        tasks={TASKS}
        relatedDocs={RELATED_DOCS}
        toc={TOC}
        comments={COMMENTS}
        canComment
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.side-panel", "fui-state--Default"],
};

export const SidePanelEmpty: Story = {
  render: () => (
    <div className="flex h-[600px]">
      <DocSidePanelSurface
        copy={docsCopy}
        tab="links"
        tasks={[]}
        relatedDocs={[]}
        toc={[]}
        comments={[]}
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.side-panel", "fui-state--Empty"],
};

export const VersionsDefault: Story = {
  render: () => (
    <div className="flex h-[900px] flex-col">
      <div className="w-[1108px]">
        <DocVersionBarSurface copy={docsCopy} version={VERSIONS[2]!} />
      </div>
      <DocVersionPanelSurface
        copy={docsCopy}
        versions={VERSIONS}
        selectedId="v3"
      />
    </div>
  ),
  tags: ["fui-surface--docs.page.versions", "fui-state--Default"],
};

export const VersionsEmpty: Story = {
  render: () => (
    <div className="flex h-[400px]">
      <DocVersionPanelSurface copy={docsCopy} versions={[]} selectedId={null} />
    </div>
  ),
  tags: ["fui-surface--docs.page.versions", "fui-state--Empty"],
};

export const ShareDefault: Story = {
  render: () => (
    <DocShareDialogSurface
      copy={docsCopy}
      pageTitle={HEADER.title}
      share={SHARE}
    />
  ),
  tags: ["fui-surface--docs.page.share", "fui-state--Default"],
};
