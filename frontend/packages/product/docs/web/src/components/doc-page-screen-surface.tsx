import { useEffect, useRef, useState } from "react";
import type { BlockType } from "@notrelix/docs-core";
import { Skeleton } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import {
  DocArticleSurface,
  type DocArticleSurfaceProps,
} from "./doc-article-surface";
import { DocPageBarSurface } from "./doc-page-bar-surface";
import type {
  DocBlockView,
  DocChip,
  DocCommentView,
  DocHeaderView,
  DocOverlay,
  DocPageCallbacks,
  DocPanelTab,
  DocRelatedDoc,
  DocSaveState,
  DocShareView,
  DocTaskRef,
  DocTaskResolution,
  DocTocEntry,
  DocVersionView,
} from "./doc-page-models";
import { DocShareDialogSurface } from "./doc-share-dialog-surface";
import { DocSidePanelSurface } from "./doc-side-panel-surface";
import {
  DEFAULT_TOOLBAR_CURRENT,
  DocToolbarSurface,
  type DocToolbarCurrent,
} from "./doc-toolbar-surface";
import {
  DocVersionBarSurface,
  DocVersionPanelSurface,
} from "./doc-versions-surface";

export interface DocVersionsState {
  readonly list: readonly DocVersionView[];
  readonly selectedId: string | null;
  readonly namedOnly: boolean;
  readonly onNamedOnlyChange?: (value: boolean) => void;
}

export interface DocPageScreenSurfaceProps {
  readonly status?: "ready" | "loading" | "notFound";
  readonly copy: DocPageCopy;
  readonly header: DocHeaderView;
  readonly saveState: DocSaveState;
  readonly chips: readonly DocChip[];
  readonly why: string | null;
  readonly blocks: readonly DocBlockView[];
  readonly tasks: readonly DocTaskResolution[];
  readonly relatedDocs: readonly DocRelatedDoc[];
  readonly toc: readonly DocTocEntry[];
  readonly comments: readonly DocCommentView[];
  readonly panelTab: DocPanelTab;
  readonly overlay: DocOverlay;
  readonly mentionTasks: readonly DocTaskRef[];
  readonly blockTypes?: readonly BlockType[];
  readonly toolbar?: DocToolbarCurrent;
  readonly versions?: DocVersionsState;
  readonly share?: DocShareView;
  readonly readOnly?: boolean;
  readonly canComment?: boolean;
  readonly canFormat?: boolean;
  readonly callbacks?: DocPageCallbacks;
  readonly onCopyLink?: () => void;
}

/**
 * The document page (Final-Docs): the document bar, the menu bar and
 * formatting toolbar, the page sheet and the side panel; version history
 * swaps the toolbar for a compare banner and the side tabs for the list.
 */
export function DocPageScreenSurface(props: DocPageScreenSurfaceProps) {
  const { copy, status = "ready" } = props;

  if (status === "loading") {
    return (
      <div
        role="status"
        aria-label={copy.loading}
        className="flex h-full min-h-0 flex-1 flex-col gap-6 bg-doc-page p-8"
      >
        <Skeleton className="h-8 w-1/3" />
        <Skeleton className="h-6 w-2/3" />
        <Skeleton className="h-6 w-1/2" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  if (status === "notFound") {
    return (
      <div className="grid h-full min-h-0 flex-1 place-items-center bg-doc-page p-8 text-center text-compact leading-[1.45] text-meta-foreground">
        {copy.notFound}
      </div>
    );
  }

  return <DocPageReady {...props} />;
}

/** The ready page; its hooks live here so loading and not-found stay hook-free. */
function DocPageReady(props: DocPageScreenSurfaceProps) {
  const { copy } = props;
  const {
    header,
    saveState,
    chips,
    why,
    blocks,
    tasks,
    relatedDocs,
    toc,
    comments,
    panelTab,
    overlay,
    mentionTasks,
    blockTypes,
    toolbar = DEFAULT_TOOLBAR_CURRENT,
    versions,
    share,
    readOnly = false,
    canComment = false,
    canFormat = false,
    callbacks,
    onCopyLink,
  } = props;

  const scroller = useRef<HTMLDivElement | null>(null);
  const [spyId, setSpyId] = useState<string | null>(null);
  const tocIds = toc.map((entry) => entry.id).join("|");

  // Scroll spy: the contents entry of the last heading above the fold.
  useEffect(() => {
    const element = scroller.current;
    if (!element) return;
    const update = () => {
      const top = element.getBoundingClientRect().top + 80;
      let current: string | null = null;
      for (const id of tocIds.split("|")) {
        if (id === "title") continue;
        const heading = element.querySelector<HTMLElement>(
          `[data-block="${CSS.escape(id)}"]`,
        );
        if (heading && heading.getBoundingClientRect().top <= top) current = id;
      }
      setSpyId(current);
    };
    update();
    element.addEventListener("scroll", update, { passive: true });
    return () => element.removeEventListener("scroll", update);
  }, [tocIds]);

  const viewing = versions
    ? (versions.list.find((entry) => entry.id === versions.selectedId) ?? null)
    : null;
  const shareOpen = overlay.kind === "share" && share !== undefined;
  const articleProps: DocArticleSurfaceProps = {
    copy,
    title: header.title,
    chips,
    why,
    blocks,
    readOnly: readOnly || versions !== undefined,
    compare: versions !== undefined,
    overlay,
    mentionTasks,
    blockTypes,
    callbacks,
  };

  return (
    <>
      <div
        className="flex h-full min-h-0 min-w-0 flex-1 flex-col bg-doc-page text-body leading-[1.45] text-foreground"
        aria-hidden={shareOpen || undefined}
        inert={shareOpen || undefined}
      >
        <DocPageBarSurface
          copy={copy}
          header={header}
          saveState={saveState}
          shareOpen={shareOpen}
          canShare={share !== undefined}
          callbacks={callbacks}
        />
        {versions ? (
          viewing ? (
            <DocVersionBarSurface
              copy={copy}
              version={viewing}
              callbacks={callbacks}
            />
          ) : null
        ) : (
          <DocToolbarSurface
            copy={copy}
            current={toolbar}
            fileMenuOpen={overlay.kind === "fileMenu"}
            downloadOpen={overlay.kind === "fileMenu" && overlay.downloadOpen}
            canShare={share !== undefined}
            canFormat={canFormat && !readOnly}
            callbacks={callbacks}
          />
        )}
        <div className="flex min-h-0 flex-1">
          <div
            ref={scroller}
            className="flex min-w-0 flex-1 justify-center overflow-auto px-8 pt-9 pb-16"
          >
            <DocArticleSurface {...articleProps} />
          </div>
          {versions ? (
            <DocVersionPanelSurface
              copy={copy}
              versions={versions.list}
              selectedId={versions.selectedId}
              namedOnly={versions.namedOnly}
              onNamedOnlyChange={versions.onNamedOnlyChange}
              callbacks={callbacks}
            />
          ) : (
            <DocSidePanelSurface
              copy={copy}
              tab={panelTab}
              tasks={tasks}
              relatedDocs={relatedDocs}
              toc={toc.map((entry) => ({
                ...entry,
                active: entry.active ?? (entry.id === spyId || undefined),
              }))}
              comments={comments}
              canComment={canComment && !readOnly}
              callbacks={callbacks}
            />
          )}
        </div>
      </div>
      {shareOpen && share ? (
        <DocShareDialogSurface
          copy={copy}
          pageTitle={header.title}
          share={share}
          callbacks={callbacks}
          onCopyLink={onCopyLink}
        />
      ) : null}
    </>
  );
}
