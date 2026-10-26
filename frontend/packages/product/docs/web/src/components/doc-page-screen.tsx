import { useDocLive } from "./use-doc-live";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  AttachmentUploads,
  createAttachmentApi,
  createUseAttachmentContent,
  createUsePageAttachments,
  useRememberPageAttachment,
  type DocsFileClient,
  createUseCreateComment,
  createUseDeleteComment,
  createUseResolveComment,
  createUseCreatePage,
  createUseDocEditor,
  createUseDocSaver,
  useRefreshPageBlocks,
  createUseVersionSnapshot,
  createUsePageVersions,
  createUseCreateVersion,
  createUseRestoreVersion,
  createUseCreatePageTemplate,
  createUseImportPage,
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
  type DocSaveState,
  type DocsApiClient,
  type PageApiEndpoints,
} from "@notrelix/docs-state";
import {
  draftEquals,
  draftOfBlock,
  plainTextOf,
  type BlockDraft,
  type CommentAnchor,
  type PageAttachment,
} from "@notrelix/docs-core";
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
  toHtml,
  toTocEntries,
  toVersionViews,
  markVersionDiff,
  toolbarCurrent,
} from "./doc-page-mapping";
import type {
  DocChip,
  DocHeaderView,
  DocImportView,
  DocOverlay,
  DocPageCallbacks,
  DocPanelTab,
  DocRelatedDoc,
  DocTaskRef,
  DocTaskResolution,
  DocPerson,
  DocMarkTargets,
} from "./doc-page-models";
import { isRestrictedTask } from "./doc-page-models";
import { readImportFile } from "../import/read-import-file";
import { DocPageScreenSurface } from "./doc-page-screen-surface";
import { useDocPanelOpen } from "./use-doc-panel-open";
import type { DocMediaHost } from "../editor/media-host";
import { PENDING_ID, anchorFromSelection } from "../editor/comment-anchors";
import { revealHeading, useDocSummary } from "../editor/outline";
import {
  revealComment,
  useDetachedComments,
  useShowCommentAnchors,
} from "../editor/use-comment-anchors";
import { useDocRichEditor } from "../editor/use-doc-rich-editor";
import { taskToken } from "./doc-page-text";

interface CreateDocPageScreenDeps {
  api: DocsApiClient & DocsFileClient;
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

function saveBlob(name: string, blob: Blob): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = name;
  anchor.click();
  URL.revokeObjectURL(url);
}

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
  const attachmentApi = createAttachmentApi(api, endpoints);
  const usePageAttachments = createUsePageAttachments(api, endpoints);
  const useAttachmentContent = createUseAttachmentContent(api, endpoints);
  const usePage = createUsePage(api, endpoints);
  const usePageBlocks = createUsePageBlocks(api, endpoints);
  const usePageBreadcrumb = createUsePageBreadcrumb(api, endpoints);
  const usePageComments = createUsePageComments(api, endpoints);
  const usePageHistory = createUsePageHistory(api, endpoints);
  const useDocEditor = createUseDocEditor(api, endpoints);
  const useDocSaver = createUseDocSaver(api, endpoints);
  const useCreateComment = createUseCreateComment(api, endpoints);
  const useDeleteComment = createUseDeleteComment(api, endpoints);
  const useResolveComment = createUseResolveComment(api, endpoints);
  const useCreatePage = createUseCreatePage(api, endpoints);
  const usePageBacklinks = createUsePageBacklinks(api, endpoints);
  const useConvertTodo = createUseConvertTodo(api, endpoints);
  const useVersionSnapshot = createUseVersionSnapshot(api, endpoints);
  const usePageVersions = createUsePageVersions(api, endpoints);
  const useCreateVersion = createUseCreateVersion(api, endpoints);
  const useRestoreVersion = createUseRestoreVersion(api, endpoints);
  const useCreatePageTemplate = createUseCreatePageTemplate(api, endpoints);
  const useImportPage = createUseImportPage(api, endpoints);
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
    const deleteComment = useDeleteComment(workspaceId, pageId);
    const resolveComment = useResolveComment(workspaceId, pageId);
    const createPage = useCreatePage(workspaceId);
    const backlinksQuery = usePageBacklinks(workspaceId, pageId);
    const convertTodo = useConvertTodo(workspaceId, pageId);
    const pageListQuery = usePageList(workspaceId);
    const archivePage = useArchivePage(workspaceId);
    const restorePage = useRestorePage(workspaceId);
    const deletePage = useDeletePage(workspaceId);
    const createVersion = useCreateVersion(workspaceId, pageId);
    const restoreVersion = useRestoreVersion(workspaceId, pageId);
    const createTemplate = useCreatePageTemplate(workspaceId);
    const importPage = useImportPage(workspaceId);
    const [importing, setImporting] = useState<DocImportView>(IDLE_IMPORT);

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
    // The selection a comment is being written on, and the thread being looked at.
    const [pendingAnchor, setPendingAnchor] = useState<CommentAnchor | null>(
      null,
    );
    const [activeCommentId, setActiveCommentId] = useState<string | null>(null);

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
    const refreshBlocks = useRefreshPageBlocks(workspaceId, pageId);
    useDocLive({
      realtime: host.realtime,
      workspaceId,
      pageId,
      refresh: refreshBlocks,
    });
    const [panelOpen, setPanelOpen] = useDocPanelOpen(host.viewerId ?? null);
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
      () =>
        toCommentViews(
          commentsQuery.data ?? [],
          copy,
          now,
          host.personFor,
          host.viewerId,
        ),
      [commentsQuery.data, copy, host.personFor, host.viewerId, now],
    );

    const versionsQuery = usePageVersions(
      workspaceId,
      pageId,
      versions !== null,
    );
    const versionList = useMemo(
      () => toVersionViews(versionsQuery.data ?? [], copy, now, host.personFor),
      [copy, versionsQuery.data, host.personFor, now],
    );
    // The version compared with the page: the newest one unless another is chosen.
    const selectedVersionId = versions
      ? (versions.selectedId ?? versionList[0]?.id ?? null)
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
    const markTargets = useMemo<DocMarkTargets>(
      () => ({
        person: host.personFor,
        page: (id) => readablePages.get(id) ?? null,
        openPage: host.openPage,
      }),
      [host.personFor, host.openPage, readablePages],
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

    const pickerBoards = host.boardChoices;

    const blockViews = useMemo(() => {
      const views = toBlockViews(blocks, {
        boards: host.boards,
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
        ? markVersionDiff(views, blocks, snapshotQuery.data.blocks)
        : views;
    }, [
      blocks,
      comments,
      host.board,
      host.boards,
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

    // The page's editor: built once per page from the loaded blocks, saved
    // by a debounced diff. The callbacks it needs are filled in below.
    const callbacksRef = useRef<DocPageCallbacks | undefined>(undefined);
    const richRef = useRef<{
      completeUpload: (
        upload: Parameters<(typeof richEditor)["completeUpload"]>[0],
        attachment: PageAttachment,
      ) => void;
      applyDrafts: (drafts: readonly BlockDraft[]) => void;
      currentDrafts: () => BlockDraft[];
    } | null>(null);
    // What a stored change replaced stays here until the member copies,
    // re-applies or drops it.
    const [recoveries, setRecoveries] = useState<
      readonly { id: string; draft: BlockDraft | null }[]
    >([]);
    const [copiedRecoveryId, setCopiedRecoveryId] = useState<string | null>(
      null,
    );
    const toastedRef = useRef(false);
    const saver = useDocSaver(workspaceId, pageId, blocksQuery.data, {
      onConflict: ({ blockId, local, server }) => {
        const changed = !local || !draftEquals(local, server);
        if (!changed) return;
        setRecoveries((current) => [
          ...current,
          { id: `${blockId}:${current.length}:${Date.now()}`, draft: local },
        ]);
        if (!toastedRef.current) {
          toastedRef.current = true;
          host.notify?.(copy.recovery.toast);
        }
      },
      onRebased: (drafts) => richRef.current?.applyDrafts(drafts),
    });
    const initialDrafts = useMemo(
      () =>
        [...blocks].sort((a, b) => a.position - b.position).map(draftOfBlock),
      [blocks],
    );
    const contextKey = useMemo(
      () => ({}),
      [host.tasks, host.tasksReady, host.board, host.boards, readablePages],
    );
    // The page's files: uploads in flight, the stored files and their bytes.
    const rememberAttachment = useRememberPageAttachment(workspaceId, pageId);
    const mediaRef = useRef({ copy, notify: host.notify });
    mediaRef.current = { copy, notify: host.notify };
    const uploads = useMemo(
      () =>
        new AttachmentUploads({
          upload: (file, options) =>
            attachmentApi.upload(pageId, file, options),
          onUploaded: (upload, attachment) => {
            rememberAttachment(attachment);
            richRef.current?.completeUpload(upload, attachment);
          },
          onFailed: (_upload, failure) => {
            const { copy: current, notify } = mediaRef.current;
            notify?.(
              fillDocCopy(current.media.upload.failed, {
                reason: current.media.upload.reasons[failure],
              }),
            );
          },
        }),
      [pageId, rememberAttachment],
    );
    useEffect(() => () => uploads.dispose(), [uploads]);
    const media = useMemo<DocMediaHost>(
      () => ({
        uploads,
        onRefused: (file, refusal) => {
          const { copy: current, notify } = mediaRef.current;
          notify?.(
            fillDocCopy(current.media.upload.refused[refusal], {
              name: file.name,
            }),
          );
        },
        useFiles: () => {
          const query = usePageAttachments(workspaceId, pageId);
          const files = useMemo(
            () => new Map((query.data ?? []).map((file) => [file.id, file])),
            [query.data],
          );
          return { files, loading: query.isLoading };
        },
        useContent: (attachmentId) => {
          const query = useAttachmentContent(workspaceId, pageId, attachmentId);
          return {
            status: query.isError ? "error" : query.data ? "ready" : "loading",
            blob: query.data ?? null,
            retry: () => void query.refetch(),
          };
        },
        download: async (attachment) => {
          try {
            saveBlob(
              attachment.fileName,
              await attachmentApi.content(pageId, attachment.id),
            );
          } catch {
            const { copy: current, notify } = mediaRef.current;
            notify?.(current.media.file.downloadFailed);
          }
        },
      }),
      [pageId, uploads, workspaceId],
    );
    const richEditor = useDocRichEditor({
      pageId,
      ready: !pageQuery.isLoading && !blocksQuery.isLoading && page !== null,
      initial: initialDrafts,
      editable: host.canEdit,
      placeholder: copy.blockPlaceholder,
      emptyPlaceholder: copy.blocks.emptyDoc,
      label: copy.blockPlaceholder,
      contextKey,
      context: {
        copy,
        readOnly: !host.canEdit,
        media,
        callbacks: {
          onOpenPagePicker: (blockId) =>
            callbacksRef.current?.onOpenPagePicker?.(blockId),
          onOpenBoardPicker: (blockId) =>
            callbacksRef.current?.onOpenBoardPicker?.(blockId),
          onOpenComment: (id) => callbacksRef.current?.onOpenComment?.(id),
        },
        resolveTask,
        viewOf: (draft: BlockDraft) =>
          toBlockViews(
            [
              {
                id: draft.id,
                pageId,
                type: draft.type,
                content: draft.content,
                properties: {
                  ...draft.properties,
                  text: plainTextOf(draft.content),
                },
                position: 0,
                parentId: null,
                createdById: "",
                updatedById: "",
                createdAt: "",
                updatedAt: "",
              },
            ],
            {
              boards: host.boards,
              resolveTask,
              pages: readablePages,
              pageHref: host.pageHref,
              board: host.board,
              quotes: [],
            },
          )[0],
      },
      onDrafts: saver.change,
    });
    richRef.current = richEditor;
    const anchoredComments = useMemo(
      () =>
        (commentsQuery.data ?? []).flatMap((comment) =>
          // A resolved thread stops marking the text.
          comment.anchor && !comment.resolved
            ? [{ id: comment.id, ...comment.anchor }]
            : [],
        ),
      [commentsQuery.data],
    );
    useShowCommentAnchors(
      richEditor.editor,
      anchoredComments,
      pendingAnchor,
      activeCommentId,
    );
    const detachedComments = useDetachedComments(richEditor.editor);
    const commentViews = useMemo(
      () =>
        comments.map((comment) => ({
          ...comment,
          ...(detachedComments.includes(comment.id) ? { detached: true } : {}),
          ...(comment.id === activeCommentId ? { active: true } : {}),
        })),
      [comments, detachedComments, activeCommentId],
    );
    const summary = useDocSummary(richEditor.editor);
    const outlineIds = (summary?.outline ?? [])
      .map((entry) => entry.id)
      .join("|");

    // The contents follow the reading position: the heading nearest the middle
    // of the visible page is the current one.
    useEffect(() => {
      if (panelTab !== "toc") return;
      const ids = outlineIds ? outlineIds.split("|") : [];
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
        for (const id of ids) {
          const element = document.querySelector(
            `[data-block="${CSS.escape(id)}"]`,
          );
          if (!element) continue;
          const distance = Math.abs(
            element.getBoundingClientRect().top - middle,
          );
          if (!best || distance < best.distance) best = { id, distance };
        }
        if (best) setActiveTocId(best.id);
      };
      update();
      target.addEventListener("scroll", update, { passive: true });
      return () => target.removeEventListener("scroll", update);
    }, [outlineIds, panelTab]);

    const recoveryViews = recoveries.map((entry) => ({
      id: entry.id,
      text: entry.draft ? plainTextOf(entry.draft.content) : "",
    }));
    const dropRecovery = (id: string) =>
      setRecoveries((current) => current.filter((entry) => entry.id !== id));

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
    const mentionPeople: readonly DocPerson[] =
      overlay.kind === "mention" && host.suggestMembers
        ? host.suggestMembers(overlay.query)
        : [];

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
              // The line is converted by its stored id: save it first.
              void saver
                .flush()
                .then(() =>
                  setOverlay({ kind: "convertTask", blockId, boardId: null }),
                )
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
      onOpenBoardPicker:
        host.canEdit && pickerBoards
          ? (blockId) => setOverlay({ kind: "boardPicker", blockId })
          : undefined,
      onPickBoard: (blockId, target) => {
        const block = blockById(blockId);
        setOverlay(NONE);
        if (richEditor.editor) {
          richEditor.updateBlockAttrs(blockId, { linkedBoardId: target });
        } else if (block) {
          patchBlock(block, { linkedBoardId: target });
        }
      },
      onPickPage: (blockId, target) => {
        const block = blockById(blockId);
        setOverlay(NONE);
        if (richEditor.editor) {
          richEditor.updateBlockAttrs(blockId, { linkedPageId: target });
        } else if (block) {
          patchBlock(block, { linkedPageId: target });
        }
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
      onSaveVersionOpen: host.canEdit
        ? () => {
            createVersion.reset();
            setOverlay({ kind: "saveVersion" });
          }
        : undefined,
      onSaveVersion: (name) => {
        void saver.flush().then(() =>
          createVersion.mutate(name, {
            onSuccess: () => {
              setOverlay(NONE);
              host.notify?.(fillDocCopy(copy.versions.saved, { name }));
            },
          }),
        );
      },
      onRestoreVersionOpen: host.canEdit
        ? () => {
            restoreVersion.reset();
            setOverlay({ kind: "restoreVersion" });
          }
        : undefined,
      onRestoreVersion: () => {
        if (!selectedVersionId) return;
        const stamp = versionList.find(
          (entry) => entry.id === selectedVersionId,
        )?.stamp;
        void saver.flush().then(() =>
          restoreVersion.mutate(selectedVersionId, {
            onSuccess: () => {
              setOverlay(NONE);
              setVersions(null);
              host.notify?.(
                fillDocCopy(copy.versions.restored, { stamp: stamp ?? "" }),
              );
            },
          }),
        );
      },
      onSaveTemplateOpen: host.canEdit
        ? () => {
            createTemplate.reset();
            setOverlay({ kind: "saveTemplate" });
          }
        : undefined,
      onSaveTemplate: (name, description) => {
        void saver.flush().then(() =>
          createTemplate.mutate(
            { pageId, name, description: description || undefined },
            {
              onSuccess: () => {
                setOverlay(NONE);
                host.notify?.(fillDocCopy(copy.template.saved, { name }));
              },
            },
          ),
        );
      },
      onImportOpen: host.canEdit
        ? () => {
            setImporting(IDLE_IMPORT);
            setOverlay({ kind: "importPage" });
          }
        : undefined,
      onImportFile: (file) => {
        setImporting({
          busy: true,
          error: null,
          fileName: file.name,
          imagesDropped: 0,
        });
        void readImportFile(file).then((read) => {
          if (!read.ok) {
            setImporting({
              busy: false,
              error: read.error,
              fileName: file.name,
              imagesDropped: 0,
            });
            return;
          }
          importPage.mutate(
            {
              title: read.title,
              blocks: read.page.blocks,
              spaceId: page.spaceId ?? null,
            },
            {
              onSuccess: (createdId) => {
                setOverlay(NONE);
                setImporting(IDLE_IMPORT);
                const dropped = read.page.imagesDropped;
                host.notify?.(
                  [
                    fillDocCopy(copy.importDialog.done, { name: read.title }),
                    dropped > 0
                      ? fillDocCopy(copy.importDialog.imagesDropped, {
                          count: String(dropped),
                        })
                      : "",
                  ]
                    .filter(Boolean)
                    .join(" "),
                );
                host.openPage?.(createdId);
              },
              onError: () =>
                setImporting({
                  busy: false,
                  error: "failed",
                  fileName: file.name,
                  imagesDropped: read.page.imagesDropped,
                }),
            },
          );
        });
      },
      onDownload: (format) => {
        setOverlay(NONE);
        if (format === "pdf") {
          window.print();
          return;
        }
        if (format === "html") {
          saveFile(
            `${title}.html`,
            "text/html",
            toHtml(
              title,
              blocks,
              resolveTask,
              document.documentElement.lang || "en",
            ),
          );
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
      onPanelOpenChange: setPanelOpen,
      onTocSelect: (id) => {
        setActiveTocId(id);
        if (
          id !== "title" &&
          richEditor.editor &&
          revealHeading(richEditor.editor, id)
        ) {
          return;
        }
        const target =
          id === "title"
            ? document.querySelector("article h1")
            : document.querySelector(`[data-block="${CSS.escape(id)}"]`);
        target?.scrollIntoView({ block: "start", behavior: "smooth" });
      },
      onSaveRetry: () => {
        editor.retry();
        void saver.retry();
      },
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
        void saver.flush();
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
      onRecoveryCopy: (id) => {
        const text = recoveries.find((entry) => entry.id === id)?.draft;
        if (!text) return;
        void navigator.clipboard
          ?.writeText(plainTextOf(text.content))
          .then(() => setCopiedRecoveryId(id))
          .catch(() => undefined);
      },
      onRecoveryReapply: (id) => {
        const draft = recoveries.find((entry) => entry.id === id)?.draft;
        const rich = richRef.current;
        if (!draft || !rich) return;
        // The member's text goes back into the block; it saves as a new
        // edit of the stored version.
        rich.applyDrafts(
          rich
            .currentDrafts()
            .map((current) =>
              current.id === draft.id
                ? { ...draft, version: undefined }
                : current,
            ),
        );
        dropRecovery(id);
      },
      onRecoveryDismiss: dropRecovery,
      onOpenComments: () => {
        setPanelTab("comments");
        setPanelOpen(true);
      },
      onCommentStart: () => {
        const anchor = richEditor.editor
          ? anchorFromSelection(richEditor.editor)
          : null;
        if (!anchor) return;
        setPendingAnchor(anchor);
        setPanelTab("comments");
        setPanelOpen(true);
      },
      onCommentCancel: () => setPendingAnchor(null),
      onCommentSelect: (id) => {
        setActiveCommentId(id);
        if (richEditor.editor) revealComment(richEditor.editor, id);
      },
      onOpenComment: (id) => {
        if (id === PENDING_ID) return;
        setActiveCommentId(id);
        setPanelTab("comments");
        setPanelOpen(true);
        document
          .getElementById(`comment-${id}`)
          ?.scrollIntoView?.({ block: "nearest" });
      },
      onCommentResolve: (id) => {
        resolveComment.mutate(id);
        if (id === activeCommentId) setActiveCommentId(null);
      },
      onCommentDelete: (id) => {
        deleteComment.mutate(id);
        if (id === activeCommentId) setActiveCommentId(null);
      },
      onCommentCreate: (body) => {
        const anchor = pendingAnchor;
        if (!anchor) {
          createComment.mutate({ pageId, body });
          return;
        }
        // The block the comment names must be stored before the producer can check it.
        void saver
          .flush()
          .then(() =>
            createComment.mutate(
              { pageId, body, anchor },
              { onSuccess: () => setPendingAnchor(null) },
            ),
          );
      },
    };

    callbacksRef.current = callbacks;
    const saveState: DocSaveState =
      editor.saveState === "error" || saver.saveState === "error"
        ? "error"
        : editor.saveState === "saving" || saver.saveState === "saving"
          ? "saving"
          : "saved";

    return (
      <DocPageScreenSurface
        copy={copy}
        header={header}
        saveState={saveState}
        editor={richEditor.editor}
        recoveries={recoveryViews}
        copiedRecoveryId={copiedRecoveryId}
        chips={chips}
        why={why}
        blocks={blockViews}
        tasks={mentionedTasks}
        relatedDocs={relatedDocs}
        toc={(summary
          ? [
              { id: "title", level: 1 as const, text: title },
              ...summary.outline.map((entry) => ({
                id: entry.id,
                level: (entry.level + 1) as 2 | 3 | 4,
                text: entry.text,
              })),
            ]
          : toTocEntries(title, blocks)
        ).map((entry) => ({
          ...entry,
          active: activeTocId ? entry.id === activeTocId : undefined,
        }))}
        stats={summary?.stats}
        comments={commentViews}
        pendingQuote={pendingAnchor?.quote ?? null}
        panelTab={panelTab}
        panelOpen={panelOpen}
        overlay={overlay}
        mentionTasks={mentionTasks}
        mentionPeople={mentionPeople}
        markTargets={markTargets}
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
        history={{
          canRestore: host.canEdit,
          saving: createVersion.isPending,
          saveFailed: createVersion.isError,
          restoring: restoreVersion.isPending,
          restoreFailed: restoreVersion.isError
            ? (restoreVersion.error as { status?: unknown }).status === 412
              ? "conflict"
              : "failed"
            : null,
        }}
        importState={importing}
        templateAction={{
          saving: createTemplate.isPending,
          failed: createTemplate.isError,
        }}
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
        pickerBoards={pickerBoards}
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

const IDLE_IMPORT: DocImportView = {
  busy: false,
  error: null,
  fileName: null,
  imagesDropped: 0,
};

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
