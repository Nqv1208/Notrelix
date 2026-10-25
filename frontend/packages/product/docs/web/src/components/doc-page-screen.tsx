import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  createUseCreateComment,
  createUseCreatePage,
  createUseDocEditor,
  createUseVersionSnapshot,
  createUsePage,
  createUsePageBlocks,
  createUseArchivePage,
  createUseConvertTodo,
  createUseDeletePage,
  createUsePageBacklinks,
  createUsePageList,
  createUseRestorePage,
  createUseTrashedPages,
  createUsePageBreadcrumb,
  createUsePageComments,
  createUsePageHistory,
  type Block,
  type BlockType,
  type DocsApiClient,
  type PageApiEndpoints,
} from "@notrelix/docs-state";
import { fillDocCopy } from "./doc-page.copy";
import { useDocPageCopy } from "./use-doc-page-copy";
import type { UseDocPageHost } from "./doc-page-host";
import {
  blockText,
  dayMonth,
  editedAtLabel,
  referencesOf,
  toBlockViews,
  toCommentViews,
  toMarkdown,
  toTocEntries,
  toVersionViews,
  markVersionDiff,
  toolbarCurrent,
} from "./doc-page-mapping";
import type {
  DocChip,
  DocHeaderView,
  DocOverlay,
  DocPageCallbacks,
  DocPanelTab,
  DocRelatedDoc,
  DocTaskRef,
  DocTaskResolution,
} from "./doc-page-models";
import { isRestrictedTask } from "./doc-page-models";
import { DocPageScreenSurface } from "./doc-page-screen-surface";
import { taskToken } from "./doc-page-text";

interface CreateDocPageScreenDeps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
  /** Host-composed facts (department, people, tasks, viewer). */
  useHost: UseDocPageHost;
}

const NONE: DocOverlay = { kind: "none" };

/** Types whose Enter makes another block of the same kind. */
const REPEATING: ReadonlySet<BlockType> = new Set([
  "numbered_list",
  "bulleted_list",
  "todo",
]);

function saveFile(name: string, mime: string, content: string): void {
  const url = URL.createObjectURL(new Blob([content], { type: mime }));
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = name;
  anchor.click();
  URL.revokeObjectURL(url);
}

/** Puts the caret at the end of a block's editable text. */
function focusBlock(blockId: string): boolean {
  const element = document.querySelector<HTMLElement>(
    `[data-block-id="${CSS.escape(blockId)}"]`,
  );
  if (!element) return false;
  element.focus();
  const selection = window.getSelection();
  if (selection) {
    const range = document.createRange();
    range.selectNodeContents(element);
    range.collapse(false);
    selection.removeAllRanges();
    selection.addRange(range);
  }
  return true;
}

export function createDocPageScreen({
  api,
  endpoints,
  useHost,
}: CreateDocPageScreenDeps) {
  const usePage = createUsePage(api, endpoints);
  const usePageBlocks = createUsePageBlocks(api, endpoints);
  const usePageBreadcrumb = createUsePageBreadcrumb(api, endpoints);
  const usePageComments = createUsePageComments(api, endpoints);
  const usePageHistory = createUsePageHistory(api, endpoints);
  const useDocEditor = createUseDocEditor(api, endpoints);
  const useCreateComment = createUseCreateComment(api, endpoints);
  const useCreatePage = createUseCreatePage(api, endpoints);
  const usePageBacklinks = createUsePageBacklinks(api, endpoints);
  const useConvertTodo = createUseConvertTodo(api, endpoints);
  const useVersionSnapshot = createUseVersionSnapshot(api, endpoints);
  const usePageList = createUsePageList(api, endpoints);
  const useArchivePage = createUseArchivePage(api, endpoints);
  const useRestorePage = createUseRestorePage(api, endpoints);
  const useDeletePage = createUseDeletePage(api, endpoints);
  const useTrashedPages = createUseTrashedPages(api, endpoints);

  return function DocPageScreen({
    workspaceId,
    pageId,
  }: {
    workspaceId: string;
    pageId: string;
  }) {
    const copy = useDocPageCopy();
    const pageQuery = usePage(workspaceId, pageId);
    const blocksQuery = usePageBlocks(workspaceId, pageId) as {
      data: Block[] | undefined;
      isLoading: boolean;
    };
    const breadcrumbQuery = usePageBreadcrumb(workspaceId, pageId);
    const commentsQuery = usePageComments(workspaceId, pageId);
    const historyQuery = usePageHistory(workspaceId, pageId);
    const editor = useDocEditor(workspaceId, pageId);
    const createComment = useCreateComment(workspaceId, pageId);
    const createPage = useCreatePage(workspaceId);
    const backlinksQuery = usePageBacklinks(workspaceId, pageId);
    const convertTodo = useConvertTodo(workspaceId, pageId);
    const pageListQuery = usePageList(workspaceId);
    const archivePage = useArchivePage(workspaceId);
    const restorePage = useRestorePage(workspaceId);
    const deletePage = useDeletePage(workspaceId);

    const page = pageQuery.data ?? null;
    const blocks = useMemo(() => blocksQuery.data ?? [], [blocksQuery.data]);

    const [overlay, setOverlay] = useState<DocOverlay>(NONE);
    const [panelTab, setPanelTab] = useState<DocPanelTab>("links");
    const [focusedId, setFocusedId] = useState<string | null>(null);
    const [versions, setVersions] = useState<{
      selectedId: string | null;
      namedOnly: boolean;
    } | null>(null);
    const [activeTocId, setActiveTocId] = useState<string | null>(null);

    // The contents follow the reading position: the heading nearest the middle
    // of the visible page is the current one.
    useEffect(() => {
      if (panelTab !== "toc") return;
      const article = document.querySelector("article");
      let scroller: HTMLElement | null = article?.parentElement ?? null;
      while (scroller && scroller.scrollHeight <= scroller.clientHeight) {
        scroller = scroller.parentElement;
      }
      const target: HTMLElement | Window = scroller ?? window;
      const update = () => {
        const box = scroller?.getBoundingClientRect();
        const top = box?.top ?? 0;
        const middle = top + (box?.height ?? window.innerHeight) / 2;
        let best: { id: string; distance: number } | null = null;
        for (const block of blocks) {
          if (!block.type.startsWith("heading")) continue;
          const element = document.querySelector(
            `[data-block="${CSS.escape(block.id)}"]`,
          );
          if (!element) continue;
          const distance = Math.abs(
            element.getBoundingClientRect().top - middle,
          );
          if (!best || distance < best.distance)
            best = { id: block.id, distance };
        }
        if (best) setActiveTocId(best.id);
      };
      update();
      target.addEventListener("scroll", update, { passive: true });
      return () => target.removeEventListener("scroll", update);
    }, [blocks, panelTab]);
    const pendingFocus = useRef<string | null>(null);

    const { taskIds, boardIds } = useMemo(() => referencesOf(blocks), [blocks]);
    const host = useHost({
      workspaceId,
      pageId,
      page,
      taskIds,
      boardIds,
      mentionQuery: overlay.kind === "mention" ? overlay.query : null,
      convertBoardId: overlay.kind === "convertTask" ? overlay.boardId : null,
    });
    const trashQuery = useTrashedPages(workspaceId, overlay.kind === "trash");
    const now = host.now;

    // A block created or retyped takes the caret once it is on screen.
    useEffect(() => {
      const target = pendingFocus.current;
      if (target && focusBlock(target)) pendingFocus.current = null;
    }, [blocks]);

    // Escape and outside clicks close the File menu.
    useEffect(() => {
      if (overlay.kind !== "fileMenu") return;
      const onKey = (event: KeyboardEvent) => {
        if (event.key === "Escape") setOverlay(NONE);
      };
      const onPointer = (event: MouseEvent) => {
        const target = event.target as Element | null;
        if (!target?.closest('[role="menu"],[aria-haspopup="menu"]')) {
          setOverlay(NONE);
        }
      };
      window.addEventListener("keydown", onKey);
      window.addEventListener("mousedown", onPointer);
      return () => {
        window.removeEventListener("keydown", onKey);
        window.removeEventListener("mousedown", onPointer);
      };
    }, [overlay.kind]);

    const resolveTask = useCallback(
      (taskId: string): DocTaskResolution => {
        const known = host.tasks.get(taskId);
        if (known) return known;
        if (host.tasksReady) return { id: taskId, restricted: true };
        return {
          id: taskId,
          key: "…",
          title: "…",
          status: "todo",
          assignee: null,
          due: null,
        };
      },
      [host.tasks, host.tasksReady],
    );

    const comments = useMemo(
      () => toCommentViews(commentsQuery.data ?? [], copy, now, host.personFor),
      [commentsQuery.data, copy, host.personFor, now],
    );

    const versionList = useMemo(
      () => toVersionViews(historyQuery.data ?? [], copy, now, host.personFor),
      [copy, historyQuery.data, host.personFor, now],
    );
    // The version compared with the page: the newest one from an earlier day
    // unless another is chosen.
    const selectedVersionId = versions
      ? (versions.selectedId ??
        versionList.find((entry) => entry.day !== versionList[0]?.day)?.id ??
        versionList[1]?.id ??
        versionList[0]?.id ??
        null)
      : null;
    const snapshotQuery = useVersionSnapshot(
      workspaceId,
      pageId,
      selectedVersionId,
    );

    // The pages the viewer can read (a page reference to any other reads as unavailable).
    const readablePages = useMemo(
      () =>
        new Map(
          (pageListQuery.data ?? []).map((entry) => [
            entry.id,
            { title: entry.title, icon: entry.icon },
          ]),
        ),
      [pageListQuery.data],
    );
    const pickerPages = useMemo(
      () =>
        (pageListQuery.data ?? [])
          .filter((entry) => entry.id !== pageId)
          .map((entry) => ({
            id: entry.id,
            title: entry.title,
            icon: entry.icon,
            detail: entry.spaceId
              ? (host.spaceNameFor?.(entry.spaceId) ?? null)
              : null,
          })),
      [host, pageId, pageListQuery.data],
    );

    const blockViews = useMemo(() => {
      const views = toBlockViews(blocks, {
        resolveTask,
        pages: readablePages,
        pageHref: host.pageHref,
        board: host.board,
        quotes:
          panelTab === "comments" && !versions
            ? comments.flatMap((comment) =>
                comment.quote ? [comment.quote] : [],
              )
            : [],
      });
      return versions && snapshotQuery.data
        ? markVersionDiff(views, blocks, snapshotQuery.data)
        : views;
    }, [
      blocks,
      comments,
      host.board,
      host.pageHref,
      panelTab,
      readablePages,
      resolveTask,
      snapshotQuery.data,
      versions,
    ]);

    // The tasks the page mentions: the ones that have the page attached are
    // listed under "Được nhắc tới ở"; open work reads before finished work and
    // a task the viewer cannot read comes last.
    const backlinkTaskIds = useMemo(() => new Set<string>(), []);
    const mentionedTasks = useMemo(() => {
      const rank = (task: DocTaskResolution): number =>
        isRestrictedTask(task)
          ? 4
          : ({ doing: 0, review: 1, todo: 2, done: 3 } as const)[task.status];
      return taskIds
        .filter((id) => !backlinkTaskIds.has(id))
        .map(resolveTask)
        .map((task, index) => ({ task, index }))
        .sort((a, b) => rank(a.task) - rank(b.task) || a.index - b.index)
        .map((entry) => entry.task);
    }, [backlinkTaskIds, resolveTask, taskIds]);
    const relatedDocs = useMemo<readonly DocRelatedDoc[]>(
      () => [
        ...(backlinksQuery.data?.docs ?? []).map((doc) => {
          const space = doc.spaceId
            ? (host.spaceNameFor?.(doc.spaceId) ?? null)
            : null;
          return {
            id: doc.id,
            kind: "doc" as const,
            title: doc.title,
            detail: space
              ? fillDocCopy(copy.panel.relatedDocDetail, { space })
              : copy.panel.relatedDocDetailNoSpace,
            href: host.pageHref?.(doc.id),
          };
        }),
      ],
      [backlinksQuery.data, copy, host],
    );

    if (pageQuery.isLoading || blocksQuery.isLoading) {
      return <DocPageScreenSurface status="loading" copy={copy} {...EMPTY} />;
    }
    if (!page) {
      return <DocPageScreenSurface status="notFound" copy={copy} {...EMPTY} />;
    }

    const title = page.title;
    const crumbs = [
      ...(host.space ? [host.space.name] : []),
      ...(breadcrumbQuery.data?.map((item) => item.title) ?? [title]),
    ];
    const latest = versionList[0];
    const latestActivity = (historyQuery.data ?? [])
      .slice()
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))[0];
    const header: DocHeaderView = {
      title,
      crumbs,
      editedBy: latestActivity
        ? host.personFor(latestActivity.actorId).name
        : null,
      editedAt: latestActivity
        ? editedAtLabel(copy, latestActivity.createdAt, now)
        : latest
          ? latest.time
          : null,
      presence: page.metadata.activeUserIds
        .filter((id) => id !== host.viewerId)
        .map(host.personFor),
    };

    const boardChipSource = host.board;
    const chips: DocChip[] = [
      ...(host.space
        ? [
            {
              id: "scope",
              kind: "scope" as const,
              label: fillDocCopy(copy.meta.chipScope, {
                space: host.space.department
                  ? fillDocCopy(copy.meta.department, {
                      name: host.space.name,
                    })
                  : host.space.name,
                visibility:
                  host.space.visibility === "private"
                    ? copy.meta.private
                    : copy.meta.workspaceVisible,
              }),
            },
          ]
        : []),
      ...(page.deadline
        ? [
            {
              id: "date",
              kind: "date" as const,
              label: fillDocCopy(copy.meta.effective, {
                date: `${dayMonth(new Date(page.deadline))}/${new Date(page.deadline).getFullYear()}`,
              }),
            },
          ]
        : []),
      ...(boardChipSource
        ? [
            {
              id: "board",
              kind: "board" as const,
              label: boardChipSource.name,
              href: boardChipSource.href,
            },
          ]
        : []),
    ];
    const why =
      host.space && host.viewerReason === "workspace-admin"
        ? fillDocCopy(copy.meta.whyAdmin, {
            space: host.space.department
              ? fillDocCopy(copy.meta.departmentInline, {
                  name: host.space.name,
                })
              : host.space.name,
          })
        : null;

    const focused = blocks.find((block) => block.id === focusedId) ?? null;
    const mentionTasks: readonly DocTaskRef[] =
      overlay.kind === "mention" ? host.suggestTasks(overlay.query) : [];

    const patchBlock = (
      block: Block,
      properties: Partial<Block["properties"]>,
    ) =>
      editor.updateBlock(block.id, {
        properties: { ...block.properties, ...properties },
      });
    const blockById = (id: string) => blocks.find((block) => block.id === id);

    const callbacks: DocPageCallbacks = {
      onTitleCommit: host.canEdit ? editor.updateTitle : undefined,
      onBlockCommit: host.canEdit
        ? (blockId, text) => {
            const block = blockById(blockId);
            if (block) patchBlock(block, { text });
          }
        : undefined,
      onBlockTrigger: host.canEdit
        ? (blockId, trigger) => setOverlay({ ...trigger, blockId })
        : undefined,
      onBlockTriggerClose: () =>
        setOverlay((current) =>
          current.kind === "slash" || current.kind === "mention"
            ? NONE
            : current,
        ),
      onFocusBlock: (blockId) => setFocusedId(blockId),
      onTodoConvert:
        host.canEdit && host.canConvertTodo !== false
          ? (blockId) =>
              setOverlay({ kind: "convertTask", blockId, boardId: null })
          : undefined,
      onConvertBoard: (boardId) =>
        setOverlay((current) =>
          current.kind === "convertTask" ? { ...current, boardId } : current,
        ),
      onConvertConfirm: (blockId, target) => {
        const block = blockById(blockId);
        if (!block) return;
        convertTodo.mutate(
          { block, target },
          { onSuccess: () => setOverlay(NONE) },
        );
      },
      onTrashPage: host.canEdit
        ? () => {
            setOverlay(NONE);
            archivePage.mutate(pageId, { onSuccess: () => host.leaveDoc?.() });
          }
        : undefined,
      onOpenTrash: () => setOverlay({ kind: "trash" }),
      onRestorePage: (id) => restorePage.mutate(id),
      onDeletePage: (id) => deletePage.mutate(id),
      onOpenPagePicker: host.canEdit
        ? (blockId) => setOverlay({ kind: "pagePicker", blockId })
        : undefined,
      onPickPage: (blockId, target) => {
        const block = blockById(blockId);
        setOverlay(NONE);
        if (block) patchBlock(block, { linkedPageId: target });
      },
      onTodoToggle: host.canEdit
        ? (blockId, checked) => {
            const block = blockById(blockId);
            if (block) patchBlock(block, { checked });
          }
        : undefined,
      onSlashSelect: (blockId, type) => {
        const block = blockById(blockId);
        if (!block) return;
        setOverlay(NONE);
        if (type === block.type) {
          pendingFocus.current = blockId;
          patchBlock(block, { text: "" });
          return;
        }
        void editor
          .convertBlock(
            block,
            type,
            type === "todo" ? { text: "", checked: false } : { text: "" },
          )
          .then((created) => {
            if (type === "page_reference") {
              setOverlay({ kind: "pagePicker", blockId: created.id });
              return;
            }
            pendingFocus.current = created.id;
          })
          .catch(() => undefined);
      },
      onMentionSelect: (blockId, taskId) => {
        const block = blockById(blockId);
        if (!block || overlay.kind !== "mention") return;
        const at = overlay.before.lastIndexOf("@");
        const head = overlay.before.slice(0, Math.max(at, 0));
        const tail = overlay.text.slice(overlay.before.length);
        const text = `${head}${taskToken(taskId)} ${tail}`;
        setOverlay(NONE);
        pendingFocus.current = blockId;
        patchBlock(block, { text });
      },
      onAddBlock: host.canEdit
        ? (type) => {
            const after = focusedId ?? blocks[blocks.length - 1]?.id ?? null;
            const reference = after ? blockById(after) : undefined;
            const nextType: BlockType =
              reference && REPEATING.has(reference.type)
                ? reference.type
                : type;
            void editor
              .createBlock({
                type: nextType,
                properties:
                  nextType === "todo"
                    ? { text: "", checked: false }
                    : { text: "" },
                afterBlockId: after,
              })
              .then((created) => {
                pendingFocus.current = created.id;
              })
              .catch(() => undefined);
          }
        : undefined,
      onBlockDeleteEmpty: host.canEdit
        ? (blockId) => {
            const sorted = [...blocks].sort((a, b) => a.position - b.position);
            const index = sorted.findIndex((block) => block.id === blockId);
            if (sorted.length <= 1) return;
            pendingFocus.current =
              sorted[index > 0 ? index - 1 : 1]?.id ?? null;
            editor.deleteBlock(blockId);
          }
        : undefined,
      onOpenFileMenu: () => setOverlay({ kind: "fileMenu" }),
      onCloseOverlay: () => setOverlay(NONE),
      onToggleDownload: () =>
        setOverlay((current) =>
          current.kind === "fileMenu"
            ? { kind: "fileMenu", downloadOpen: !current.downloadOpen }
            : current,
        ),
      onDownload: (format) => {
        setOverlay(NONE);
        if (format === "pdf") {
          window.print();
          return;
        }
        saveFile(
          `${title}.md`,
          "text/markdown",
          toMarkdown(title, blocks, resolveTask),
        );
      },
      onPrint: () => {
        setOverlay(NONE);
        window.print();
      },
      onNewDoc: host.canEdit
        ? () => {
            setOverlay(NONE);
            createPage.mutate(
              {
                title: copy.untitled,
                workspaceId,
                parentId: page.parentId,
                spaceId: page.spaceId ?? null,
              },
              { onSuccess: (created) => host.openPage?.(created.id) },
            );
          }
        : undefined,
      onOpenShare: host.share ? () => setOverlay({ kind: "share" }) : undefined,
      onShareInvite: host.onShareInvite,
      onPanelTab: setPanelTab,
      onTocSelect: (id) => {
        setActiveTocId(id);
        const target =
          id === "title"
            ? document.querySelector("article h1")
            : document.querySelector(`[data-block="${CSS.escape(id)}"]`);
        target?.scrollIntoView({ block: "start", behavior: "smooth" });
      },
      onSaveRetry: editor.retry,
      onFormat: (format) => {
        if (!focused) return;
        if (format === "left" || format === "center" || format === "right") {
          patchBlock(focused, { align: format });
          return;
        }
        patchBlock(focused, {
          [format === "strike" ? "strike" : format]:
            !focused.properties[format === "strike" ? "strike" : format],
        });
      },
      onParagraphStyle: (type) => {
        if (!focused || focused.type === type) return;
        void editor
          .convertBlock(focused, type, focused.properties)
          .then((created) => {
            pendingFocus.current = created.id;
          })
          .catch(() => undefined);
      },
      onOpenVersions: () => {
        setOverlay(NONE);
        setVersions({ selectedId: null, namedOnly: false });
      },
      onCloseVersions: () => setVersions(null),
      onSelectVersion: (id) =>
        setVersions((current) =>
          current ? { ...current, selectedId: id } : current,
        ),
      onMentionInsert: () => {
        const target = focusedId ?? blocks[blocks.length - 1]?.id ?? null;
        if (target) {
          const current = blockById(target);
          const base = current ? blockText(current) : "";
          setOverlay({
            kind: "mention",
            blockId: target,
            query: "",
            text: `${base}@`,
            before: `${base}@`,
          });
        }
      },
      onOpenComments: () => setPanelTab("comments"),
      onCommentCreate: (body) => createComment.mutate({ pageId, body }),
    };

    return (
      <DocPageScreenSurface
        copy={copy}
        header={header}
        saveState={editor.saveState}
        chips={chips}
        why={why}
        blocks={blockViews}
        tasks={mentionedTasks}
        relatedDocs={relatedDocs}
        toc={toTocEntries(title, blocks).map((entry) => ({
          ...entry,
          active: activeTocId ? entry.id === activeTocId : undefined,
        }))}
        comments={comments}
        panelTab={panelTab}
        overlay={overlay}
        mentionTasks={mentionTasks}
        blockTypes={host.blockTypes}
        toolbar={toolbarCurrent(
          focused ??
            [...blocks].sort((x, y) => x.position - y.position)[0] ??
            null,
        )}
        versions={
          versions
            ? {
                list: versionList,
                selectedId: selectedVersionId,
                namedOnly: versions.namedOnly,
                onNamedOnlyChange: (namedOnly) =>
                  setVersions((current) =>
                    current ? { ...current, namedOnly } : current,
                  ),
              }
            : undefined
        }
        share={
          host.share
            ? {
                ...host.share,
                space: {
                  ...host.share.space,
                  name: host.share.space.department
                    ? fillDocCopy(copy.meta.department, {
                        name: host.share.space.name,
                      })
                    : host.share.space.name,
                },
              }
            : undefined
        }
        trash={{
          pages: (trashQuery.data ?? []).map((entry) => ({
            id: entry.id,
            title: entry.title,
            icon: entry.icon,
          })),
          loading: trashQuery.isLoading,
          failed: restorePage.isError || deletePage.isError,
          busyId:
            (restorePage.isPending ? restorePage.variables : null) ??
            (deletePage.isPending ? deletePage.variables : null) ??
            null,
        }}
        pickerPages={pickerPages}
        convert={{
          boards: host.convertBoards ?? [],
          groups: host.convertGroups ?? null,
          boardId: overlay.kind === "convertTask" ? overlay.boardId : null,
          lineText:
            overlay.kind === "convertTask"
              ? (blockById(overlay.blockId)?.properties.text ?? "")
              : "",
          busy: convertTodo.isPending,
          failed: convertTodo.isError,
        }}
        readOnly={!host.canEdit}
        canComment
        canFormat={host.canEdit}
        callbacks={callbacks}
        onCopyLink={() => {
          void navigator.clipboard?.writeText(window.location.href);
        }}
      />
    );
  };
}

const EMPTY = {
  header: {
    title: "",
    crumbs: [],
    editedBy: null,
    editedAt: null,
    presence: [],
  },
  saveState: "saved",
  chips: [],
  why: null,
  blocks: [],
  tasks: [],
  relatedDocs: [],
  toc: [],
  comments: [],
  panelTab: "links",
  overlay: NONE,
  mentionTasks: [],
} as const;
