import { useEffect, useRef, useState } from "react";
import type { Editor } from "@tiptap/react";
import type { BlockType } from "@notrelix/docs-core";
import { ChevronLeft } from "lucide-react";
import { Skeleton, useIsMobile } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import {
  DocArticleSurface,
  type DocArticleSurfaceProps,
} from "./doc-article-surface";
import { DocPageBarSurface } from "./doc-page-bar-surface";
import {
  DocPageSetupDialogSurface,
  DocShortcutsDialogSurface,
  DocStatsDialogSurface,
  type PageSetup,
} from "./doc-tools-dialogs-surface";
import type { InsertHosts } from "../editor/block-insert";
import type { DocStatsView } from "../editor/doc-stats";
import type {
  DocBlockView,
  DocChip,
  DocCommentView,
  DocConvertView,
  DocMarkTargets,
  DocPerson,
  DocPickerPageView,
  DocTrashView,
  DocHeaderView,
  DocOverlay,
  DocPageCallbacks,
  DocPickerBoardView,
  DocRecoveryView,
  DocPanelTab,
  DocRelatedDoc,
  DocSaveState,
  DocShareView,
  DocTaskRef,
  DocTaskResolution,
  DocTocEntry,
  DocVersionView,
  DocHistoryActionView,
  DocImportView,
  DocTemplateActionView,
} from "./doc-page-models";
import {
  DocBoardPickerDialogSurface,
  DocConvertTaskDialogSurface,
  DocPagePickerDialogSurface,
  DocTrashDialogSurface,
} from "./doc-dialogs-surface";
import {
  DocImportDialogSurface,
  DocRestoreVersionDialogSurface,
  DocSaveTemplateDialogSurface,
  DocSaveVersionDialogSurface,
} from "./doc-history-dialogs-surface";
import { DocShareDialogSurface } from "./doc-share-dialog-surface";
import { DocMoreSheet } from "./doc-bottom-sheet-surface";
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
  /** Word and character counts of the page, shown by the document bar. */
  readonly stats?: { readonly words: number; readonly characters: number };
  readonly comments: readonly DocCommentView[];
  /** The selected text a comment is being written on. */
  readonly pendingQuote?: string | null;
  readonly panelTab: DocPanelTab;
  /** Whether the side panel shows; the host owns and remembers it. */
  readonly panelOpen?: boolean;
  readonly overlay: DocOverlay;
  readonly mentionTasks: readonly DocTaskRef[];
  /** Members the typed "@" may mention. */
  readonly mentionPeople?: readonly DocPerson[];
  /** Where mention chips and page references in the text lead. */
  readonly markTargets?: DocMarkTargets;
  readonly blockTypes?: readonly BlockType[];
  readonly toolbar?: DocToolbarCurrent;
  /** The page's editor; absent keeps the plain per-block surface. */
  readonly editor?: Editor | null;
  readonly recoveries?: readonly DocRecoveryView[];
  readonly copiedRecoveryId?: string | null;
  readonly versions?: DocVersionsState;
  /** The history dialogs' write state (save version, restore). */
  readonly history?: DocHistoryActionView;
  /** The "Save as template" dialog's write state. */
  readonly templateAction?: DocTemplateActionView;
  /** The import dialog's state. */
  readonly importState?: DocImportView;
  readonly share?: DocShareView;
  /** The trash list the trash dialog shows. */
  readonly trash?: DocTrashView;
  /** The pages the page picker offers. */
  readonly pickerPages?: readonly DocPickerPageView[];
  /** The boards the board picker offers. */
  readonly pickerBoards?: readonly DocPickerBoardView[];
  /** The convert-to-task dialog's boards and groups. */
  readonly convert?: DocConvertView;
  readonly readOnly?: boolean;
  readonly canComment?: boolean;
  readonly canFormat?: boolean;
  readonly callbacks?: DocPageCallbacks;
  /** Names where Back leads on a phone ("Quay lại <department>"). */
  readonly backLabel?: string;
  readonly onCopyLink?: () => void;
  /** The menu bar's view state and the data of its tool dialogs (needs an editor). */
  readonly tools?: DocPageTools;
}

export interface DocPageTools {
  readonly view: {
    readonly fullWidth: boolean;
    readonly focusMode: boolean;
    readonly toolbarShown: boolean;
    readonly spellcheck: boolean;
  };
  readonly hosts: InsertHosts;
  readonly onSpellcheck: (on: boolean) => void;
  /** Reads the word count of the page now (the editor's current text). */
  readonly stats: () => DocStatsView | null;
  readonly info?: {
    readonly editedBy: string | null;
    readonly editedAt: string | null;
  };
  readonly pageSetup: PageSetup;
  readonly onPageSetupChange: (setup: PageSetup) => void;
}

/**
 * The document page (Final-Docs): the document bar, the menu bar and
 * formatting toolbar, the page sheet and the side panel; version history
 * swaps the toolbar for a compare banner and the side tabs for the list.
 */
export function DocPageScreenSurface(props: DocPageScreenSurfaceProps) {
  const { copy, status = "ready" } = props;
  const phone = useIsMobile();
  // A phone has no sidebar on this page: loading and not-found keep a Back.
  const back =
    phone && props.callbacks?.onBack ? (
      <header className="box-border flex h-13 shrink-0 items-center border-b border-border px-1.5">
        <button
          type="button"
          aria-label={props.backLabel ?? copy.header.back}
          onClick={props.callbacks.onBack}
          className="grid size-11 shrink-0 place-items-center rounded-[10px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          <ChevronLeft aria-hidden="true" className="size-5 stroke-[1.8]" />
        </button>
      </header>
    ) : null;

  if (status === "loading") {
    return (
      <div className="flex h-full min-h-0 flex-1 flex-col bg-doc-page">
        {back}
        <div
          role="status"
          aria-label={copy.loading}
          className="flex min-h-0 flex-1 flex-col gap-6 p-8 max-md:p-4"
        >
          <Skeleton className="h-8 w-1/3" />
          <Skeleton className="h-6 w-2/3" />
          <Skeleton className="h-6 w-1/2" />
          <Skeleton className="h-40 w-full" />
        </div>
      </div>
    );
  }

  if (status === "notFound") {
    return (
      <div className="flex h-full min-h-0 flex-1 flex-col bg-doc-page">
        {back}
        <div className="grid min-h-0 flex-1 place-items-center p-8 text-center text-compact leading-[1.45] text-meta-foreground">
          {copy.notFound}
        </div>
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
    stats,
    comments,
    pendingQuote,
    panelTab,
    panelOpen = true,
    overlay,
    mentionTasks,
    mentionPeople,
    markTargets,
    blockTypes,
    toolbar = DEFAULT_TOOLBAR_CURRENT,
    editor,
    recoveries,
    copiedRecoveryId,
    versions,
    history,
    templateAction,
    importState,
    share,
    trash,
    pickerPages,
    pickerBoards,
    convert,
    readOnly = false,
    canComment = false,
    canFormat = false,
    callbacks,
    onCopyLink,
    tools,
  } = props;
  const focusMode = tools?.view.focusMode === true;
  const compact = useIsMobile();
  const [moreOpen, setMoreOpen] = useState(false);
  // The page goes inert under a sheet, which drops focus; remember where it was.
  const lastFocus = useRef<HTMLElement | null>(null);

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
  const sheetOpen = compact && !versions && !focusMode && panelOpen;

  const sheetShown = (sheetOpen || (compact && moreOpen)) === true;
  const wasShown = useRef(false);
  useEffect(() => {
    if (wasShown.current && !sheetShown) {
      window.requestAnimationFrame(() => lastFocus.current?.focus());
    }
    wasShown.current = sheetShown;
  }, [sheetShown]);

  const dialogOpen =
    sheetOpen ||
    (compact && moreOpen) ||
    shareOpen ||
    (overlay.kind === "trash" && trash !== undefined) ||
    (overlay.kind === "saveVersion" && history !== undefined) ||
    (overlay.kind === "restoreVersion" && history !== undefined) ||
    (overlay.kind === "saveTemplate" && templateAction !== undefined) ||
    (overlay.kind === "importPage" && importState !== undefined) ||
    (overlay.kind === "pagePicker" && pickerPages !== undefined) ||
    (overlay.kind === "boardPicker" && pickerBoards !== undefined) ||
    (overlay.kind === "convertTask" && convert !== undefined) ||
    (tools !== undefined &&
      (overlay.kind === "shortcuts" ||
        overlay.kind === "wordCount" ||
        overlay.kind === "pageInfo" ||
        overlay.kind === "pageSetup"));
  const articleProps: DocArticleSurfaceProps = {
    copy,
    title: header.title,
    chips,
    why,
    blocks,
    readOnly: readOnly || versions !== undefined,
    compare: versions !== undefined,
    fullWidth: tools?.view.fullWidth === true,
    overlay,
    mentionTasks,
    mentionPeople,
    markTargets,
    blockTypes,
    editor,
    recoveries,
    copiedRecoveryId,
    callbacks,
  };

  return (
    <>
      <div
        className="flex h-full min-h-0 min-w-0 flex-1 flex-col bg-doc-page text-body leading-[1.45] text-foreground"
        aria-hidden={dialogOpen || undefined}
        inert={dialogOpen || undefined}
        onFocusCapture={(event) => {
          lastFocus.current = event.target as HTMLElement;
        }}
      >
        {focusMode ? (
          <button
            type="button"
            onClick={callbacks?.onToggleFocusMode}
            className="fixed top-3 right-4 z-dropdown inline-flex h-8 items-center gap-1.5 rounded-[8px] border border-line-strong bg-doc-panel px-3 text-compact font-medium text-foreground shadow-popover outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {copy.menubar.view.exitFocus}
          </button>
        ) : (
          <DocPageBarSurface
            copy={copy}
            header={header}
            saveState={saveState}
            stats={stats}
            shareOpen={shareOpen}
            canShare={share !== undefined}
            panelOpen={versions ? undefined : panelOpen}
            compact={compact}
            backLabel={props.backLabel}
            commentCount={
              comments.filter((comment) => !comment.resolved).length
            }
            onOpenMore={() => setMoreOpen(true)}
            callbacks={callbacks}
          />
        )}
        {focusMode ? null : versions ? (
          viewing ? (
            <DocVersionBarSurface
              copy={copy}
              version={viewing}
              canRestore={history?.canRestore ?? false}
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
            editor={editor}
            callbacks={callbacks}
            menu={
              tools
                ? {
                    view: {
                      ...tools.view,
                      panelOpen,
                      panelTab,
                    },
                    hosts: tools.hosts,
                    onSpellcheck: tools.onSpellcheck,
                  }
                : undefined
            }
          />
        )}
        <div className="flex min-h-0 flex-1">
          <div
            ref={scroller}
            className="flex min-w-0 flex-1 justify-center overflow-auto px-8 pt-9 pb-16 max-md:justify-start max-md:px-4 max-md:pt-5 max-md:pb-[calc(48px+env(safe-area-inset-bottom,0px))]"
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
          ) : panelOpen && !focusMode && !compact ? (
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
              pendingQuote={pendingQuote}
              canComment={canComment && !readOnly}
              callbacks={callbacks}
            />
          ) : null}
        </div>
      </div>
      {sheetOpen ? (
        <DocSidePanelSurface
          sheet
          copy={copy}
          tab={panelTab}
          tasks={tasks}
          relatedDocs={relatedDocs}
          toc={toc.map((entry) => ({
            ...entry,
            active: entry.active ?? (entry.id === spyId || undefined),
          }))}
          comments={comments}
          pendingQuote={pendingQuote}
          canComment={canComment && !readOnly}
          callbacks={callbacks}
        />
      ) : null}
      {compact && moreOpen ? (
        <DocMoreSheet
          title={copy.header.moreTitle}
          closeLabel={copy.header.closeSheet}
          onClose={() => setMoreOpen(false)}
          items={[
            {
              id: "share",
              label: copy.header.share,
              unavailable: copy.header.shareUnavailable,
              onSelect:
                share !== undefined && callbacks?.onOpenShare
                  ? () => {
                      setMoreOpen(false);
                      callbacks.onOpenShare?.();
                    }
                  : undefined,
            },
            {
              id: "approve",
              label: copy.header.approve,
              unavailable: copy.header.approveUnavailable,
            },
            {
              id: "toc",
              label: copy.panel.toc,
              onSelect: () => {
                setMoreOpen(false);
                callbacks?.onPanelTab?.("toc");
                callbacks?.onPanelOpenChange?.(true);
              },
            },
            {
              id: "history",
              label: copy.fileMenu.versionHistory,
              unavailable: copy.header.moreUnavailable,
              onSelect: callbacks?.onOpenVersions
                ? () => {
                    setMoreOpen(false);
                    callbacks.onOpenVersions?.();
                  }
                : undefined,
            },
          ]}
        />
      ) : null}
      {overlay.kind === "trash" && trash ? (
        <DocTrashDialogSurface
          copy={copy}
          trash={trash}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "saveVersion" && history ? (
        <DocSaveVersionDialogSurface
          copy={copy}
          action={history}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "restoreVersion" && history && viewing ? (
        <DocRestoreVersionDialogSurface
          copy={copy}
          stamp={viewing.stamp}
          action={history}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "saveTemplate" && templateAction ? (
        <DocSaveTemplateDialogSurface
          copy={copy}
          action={templateAction}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "importPage" && importState ? (
        <DocImportDialogSurface
          copy={copy}
          state={importState}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "pagePicker" && pickerPages ? (
        <DocPagePickerDialogSurface
          copy={copy}
          blockId={overlay.blockId}
          pages={pickerPages}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "boardPicker" && pickerBoards ? (
        <DocBoardPickerDialogSurface
          copy={copy}
          blockId={overlay.blockId}
          boards={pickerBoards}
          callbacks={callbacks}
        />
      ) : null}
      {overlay.kind === "convertTask" && convert ? (
        <DocConvertTaskDialogSurface
          copy={copy}
          blockId={overlay.blockId}
          convert={convert}
          callbacks={callbacks}
        />
      ) : null}
      {tools && overlay.kind === "shortcuts" ? (
        <DocShortcutsDialogSurface copy={copy} callbacks={callbacks} />
      ) : null}
      {tools && (overlay.kind === "wordCount" || overlay.kind === "pageInfo")
        ? (() => {
            const stats = tools.stats();
            return stats ? (
              <DocStatsDialogSurface
                copy={copy}
                stats={stats}
                info={
                  overlay.kind === "pageInfo"
                    ? (tools.info ?? { editedBy: null, editedAt: null })
                    : undefined
                }
                callbacks={callbacks}
              />
            ) : null;
          })()
        : null}
      {tools && overlay.kind === "pageSetup" ? (
        <DocPageSetupDialogSurface
          copy={copy}
          setup={tools.pageSetup}
          onChange={tools.onPageSetupChange}
          callbacks={callbacks}
        />
      ) : null}
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
