import { useEffect, useMemo, useRef, useState } from "react";
import {
  useArchiveCard,
  useBoardAccess,
  useCard,
  useCardActivity,
  useCardComments,
  useCardFiles,
  useCreateCardUpdate,
  useDeleteCardUpdate,
  useDuplicateCard,
  useAddItemFile,
  useFullBoard,
  useItemChildItems,
  useItemFollowing,
  useItemLifecycle,
  usePeopleDirectory,
  useOfflineWrites,
  useItemLinks,
  useUpdateCard,
  useUpdateCardUpdate,
  useUpdateFieldValue,
  useUpdateItemStatus,
} from "@notrelix/work-management-state";
import {
  groupCardsByStatus,
  personInitials,
  type Board,
  type CardDetail,
  type UpdateCardInput,
} from "@notrelix/work-management-core";
import { boardCapabilities } from "../shared/board-capabilities";
import { formatItemDate } from "../shared/item-display";
import { useItemSheetCopy } from "../card-detail/use-item-sheet-copy";
import type { ItemSheetCopy } from "../card-detail/item-sheet.copy";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import { findItemFields, isItemDone } from "../card-detail/item-field-editors";
import { withBoardPriority } from "../shared/item-priority";
import type { TaskDetailCallbacks } from "../card-detail/task-detail-types";
import { useItemPageCopy } from "./use-item-page-copy";
import { formatFileSize } from "./item-page-format";
import { ItemPageSurface } from "./item-page-surface";
import type {
  ItemPageConflict,
  ItemPageNavigation,
  ItemPagePerson,
  ItemPageProperty,
  ItemPagePosition,
  ItemPageUpload,
  ItemPageSpace,
  ItemPageTab,
  ItemPageViewKind,
} from "./item-page-types";

/** A notice the page asks the host to show (the page owns the wording and the actions). */
export interface ItemPageToast {
  readonly kind: "success" | "error" | "info";
  readonly title: string;
  /** The part of the title drawn underlined: an item key. */
  readonly emphasis?: string;
  readonly description?: string;
  /** Label of the button that closes a notice that does not go by itself. */
  readonly dismissLabel: string;
  readonly action?: {
    readonly label: string;
    /** The key that does the same, shown beside the label. */
    readonly hint?: string;
    readonly onAction: () => void;
  };
}

/** What the host (the route) knows about where the page sits. */
export interface ItemPageHost {
  readonly space?: ItemPageSpace;
  readonly viewer?: ItemPagePerson;
  readonly boardHref?: string;
  readonly viewHrefs?: ItemPageNavigation["viewHrefs"];
  readonly onBack: () => void;
  readonly onOpenSpace?: () => void;
  readonly onOpenBoard?: () => void;
  /** The board with the side sheet open on the item. */
  readonly onCollapse?: () => void;
  readonly onOpenView?: (view: ItemPageViewKind) => void;
  /** Steps to another item of the lane (K, J); replaces the page's history entry. */
  readonly onOpenItem: (itemId: string) => void;
  readonly onCopyLink?: (itemId: string) => void;
  /** Shows a notice: an undo after a status change, a write that was not saved. */
  readonly onToast?: (toast: ItemPageToast) => void;
  /** Opens the board settings where the status list is edited. */
  readonly onEditStatuses?: () => void;
  /** Opens the command palette ("Tìm bằng ⌘K" of the not-found view). */
  readonly onOpenSearch?: () => void;
  /** Goes to the workspace home ("Về trang chủ" of the not-found view). */
  readonly onGoHome?: () => void;
}

export interface ItemPageProps extends ItemPageHost {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly itemId: string;
  readonly activeTab: ItemPageTab;
  readonly onSelectTab: (tab: ItemPageTab) => void;
}

/** A write the server refused with a conflict (HTTP 409 as an `AppError`). */
function isConflictError(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    (error as { kind?: unknown }).kind === "conflict"
  );
}

/** A failed read that says the item is not there or not for this viewer. */
function isUnseenError(error: unknown): boolean {
  const kind =
    typeof error === "object" && error !== null
      ? (error as { kind?: unknown }).kind
      : undefined;
  return kind === "not_found" || kind === "forbidden";
}

/** The item key when the link carries one ("ERP-171"), else none. */
function keyOfLink(itemId: string): string | null {
  return /^[A-Za-z][A-Za-z0-9]*-\d+$/.test(itemId)
    ? itemId.toUpperCase()
    : null;
}

/** The most a single file can be: "Vượt giới hạn 100 MB mỗi tệp". */
const MAX_UPLOAD_BYTES = 100 * 1024 * 1024;
/** How long a finished upload row stays before the saved file replaces it. */
const UPLOAD_DONE_MS = 2_500;

/** Waiting on the network: loading has taken longer than expected. */
const SLOW_LOADING_MS = 8_000;

/**
 * The item as the page shows it: loads the item and its board, and wires the
 * same commands the side sheet uses. A write that comes back as a conflict
 * keeps both values on screen until the viewer picks one.
 */
export function ItemPage(props: ItemPageProps) {
  const { workspaceId, boardId, itemId } = props;
  const sheetCopy = useItemSheetCopy();
  const copy = useItemPageCopy();
  const [now] = useState(() => new Date());
  const {
    board,
    groups,
    isLoading: boardLoading,
  } = useFullBoard(boardId, workspaceId);
  const { card, isLoading: cardLoading, error } = useCard(itemId, workspaceId);
  const { access } = useBoardAccess(boardId, workspaceId);
  const capabilities = boardCapabilities(access?.myLevel);

  // An item that belongs to another board, or does not load, is not this page's.
  const unseen = isUnseenError(error);
  const missing =
    Boolean(error) ||
    (card !== undefined &&
      (card.boardId !== boardId || card.workspaceId !== workspaceId));

  const loading = boardLoading || cardLoading || !board;
  const [slow, setSlow] = useState(false);
  useEffect(() => {
    if (!loading) {
      setSlow(false);
      return;
    }
    const timer = setTimeout(() => setSlow(true), SLOW_LOADING_MS);
    return () => clearTimeout(timer);
  }, [loading]);

  const navigation = (
    position: ItemPagePosition | undefined,
  ): ItemPageNavigation => ({
    position,
    boardHref: props.boardHref,
    viewHrefs: props.viewHrefs,
    onBack: props.onBack,
    onOpenBoard: props.onOpenBoard,
    onOpenSpace: props.onOpenSpace,
    onCollapse: props.onCollapse,
    onOpenView: props.onOpenView,
    onEditStatuses: props.onEditStatuses,
  });

  if (boardLoading || cardLoading || !board)
    return (
      <ItemPageSurface
        status={error ? "error" : "loading"}
        board={board ?? placeholderBoard(boardId, workspaceId)}
        card={null}
        capabilities={capabilities}
        callbacks={emptyCallbacks(props.onBack)}
        navigation={navigation(undefined)}
        copy={copy}
        sheetCopy={sheetCopy}
        now={now}
        space={props.space}
        itemKey={keyOfLink(itemId)}
        slow={slow}
      />
    );
  if (missing || !card)
    return (
      <ItemPageSurface
        status={unseen || (card && !error) ? "notFound" : "error"}
        itemKey={keyOfLink(itemId)}
        space={props.space}
        onOpenSearch={props.onOpenSearch}
        onGoHome={props.onGoHome}
        board={board}
        card={null}
        capabilities={capabilities}
        callbacks={emptyCallbacks(props.onBack)}
        navigation={navigation(undefined)}
        copy={copy}
        sheetCopy={sheetCopy}
        now={now}
      />
    );

  return (
    <ItemPageReady
      {...props}
      board={board}
      groups={groups}
      card={withBoardPriority(card, board)}
      capabilities={capabilities}
      access={access}
      copy={copy}
      sheetCopy={sheetCopy}
      now={now}
      navigation={navigation}
    />
  );
}

function ItemPageReady({
  board,
  groups: boardGroups,
  card,
  capabilities,
  access,
  copy,
  sheetCopy,
  now,
  navigation,
  space,
  viewer,
  activeTab,
  onSelectTab,
  onBack,
  onOpenItem,
  onCopyLink,
  onToast,
}: ItemPageProps & {
  board: Board;
  groups: ReturnType<typeof useFullBoard>["groups"];
  card: CardDetail;
  capabilities: ReturnType<typeof boardCapabilities>;
  access: ReturnType<typeof useBoardAccess>["access"];
  copy: ReturnType<typeof useItemPageCopy>;
  sheetCopy: ItemSheetCopy;
  now: Date;
  navigation: (position: ItemPagePosition | undefined) => ItemPageNavigation;
}) {
  // Files on their way: one row each, kept until sent, skipped or dismissed.
  const [uploads, setUploads] = useState<ItemPageUpload[]>([]);
  const queued = useRef(new Map<string, File>());
  const files = useCardFiles(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const archiveCard = useArchiveCard(card.boardId, card.workspaceId);
  const duplicateCard = useDuplicateCard(card.boardId, card.workspaceId);
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);
  const children = useItemChildItems(card.id, card.workspaceId, card.boardId);
  const follow = useItemFollowing(card.id, card.workspaceId);
  const links = useItemLinks(card.id, card.workspaceId, card.boardId);
  const addFile = useAddItemFile(card.id, card.workspaceId);
  const patchUpload = (id: string, patch: Partial<ItemPageUpload>) =>
    setUploads((list) =>
      list.map((entry) => (entry.id === id ? { ...entry, ...patch } : entry)),
    );
  const sendFile = (file: File, id: string) => {
    queued.current.set(id, file);
    patchUpload(id, { status: "uploading", error: null, progress: null });
    addFile
      .mutateAsync({
        filename: file.name,
        sizeBytes: file.size,
        contentType: file.type || "application/octet-stream",
      })
      .then(() => {
        queued.current.delete(id);
        patchUpload(id, { status: "done" });
        setTimeout(
          () => setUploads((list) => list.filter((entry) => entry.id !== id)),
          UPLOAD_DONE_MS,
        );
      })
      .catch(() => patchUpload(id, { status: "error", error: "failed" }));
  };
  const startUpload = (file: File) => {
    const id = crypto.randomUUID();
    const row = { id, name: file.name, size: file.size };
    if (file.size > MAX_UPLOAD_BYTES) {
      queued.current.set(id, file);
      setUploads((list) => [
        ...list,
        {
          ...row,
          status: "error",
          error: "tooBig",
          limit: formatFileSize(MAX_UPLOAD_BYTES, sheetCopy.locale),
        },
      ]);
      return;
    }
    setUploads((list) => [...list, { ...row, status: "uploading" }]);
    sendFile(file, id);
  };
  const { lifecycle, restore, reopen } = useItemLifecycle(
    card.id,
    card.workspaceId,
    card.boardId,
  );
  // Writes the network holds back: what the offline notice counts and marks.
  const { online, paused } = useOfflineWrites();
  const { directory, invitePerson } = usePeopleDirectory(
    card.boardId,
    card.workspaceId,
  );

  // The write the server refused, kept so the viewer can send it again.
  const [refused, setRefused] = useState<RefusedWrite | null>(null);
  const fields = findItemFields(board);
  const propertyOf = (fieldId: string): ItemPageProperty | null =>
    fieldId === fields.status?.id
      ? "status"
      : fieldId === fields.person?.id
        ? "assignee"
        : fieldId === fields.priority?.id
          ? "priority"
          : fieldId === fields.due?.id
            ? "due"
            : null;
  const refuse = (
    error: unknown,
    write: Omit<RefusedWrite, "fieldName" | "mine">,
    mine: unknown,
  ) => {
    const fieldName =
      write.property === "startDate"
        ? copy.rail.startDate
        : write.property === "due" && !fields.due
          ? copy.rail.due
          : (board.fieldDefinitions.find((field) => field.id === write.fieldId)
              ?.name ?? "");
    if (isConflictError(error)) {
      setRefused({ ...write, fieldName, mine });
      return;
    }
    // Not a conflict: the stored value stands, and the notice says which.
    const stored = describeConflict(
      board,
      card,
      { ...write, fieldName, mine },
      now,
    ).theirs;
    onToast?.({
      kind: "error",
      dismissLabel: copy.toasts.dismiss,
      title: fillSheetCopy(copy.toasts.saveFailed, { field: fieldName }),
      description:
        stored && stored !== "—"
          ? fillSheetCopy(copy.toasts.saveFailedBody, { value: stored })
          : copy.toasts.saveFailedNoValue,
      action: { label: copy.toasts.retry, onAction: write.send },
    });
  };
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId, {
    onError: (writeError, variables) => {
      const property = propertyOf(variables.fieldDefinitionId);
      if (property)
        refuse(
          writeError,
          {
            property,
            fieldId: variables.fieldDefinitionId,
            send: () => updateFieldValue.mutate(variables),
          },
          variables.value,
        );
    },
  });
  const updateStatus = useUpdateItemStatus(card.boardId, card.workspaceId, {
    onError: (writeError, variables) =>
      refuse(
        writeError,
        {
          property: "status",
          fieldId: fields.status?.id,
          send: () => updateStatus.mutate(variables),
        },
        variables.statusOptionId,
      ),
  });
  /** A status change from the page, with the notice that undoes it. */
  const sendStatus = (cardId: string, statusOptionId: string) => {
    const before = card.statusOptionId ?? card.status ?? null;
    const label = fields.status?.options.find(
      (option) => option.id === statusOptionId,
    )?.label;
    updateStatus.mutate(
      { cardId, statusOptionId },
      {
        onSuccess: () => {
          if (!label || before === statusOptionId) return;
          const key = card.itemKey ?? card.title;
          onToast?.({
            kind: "success",
            dismissLabel: copy.toasts.dismiss,
            title: fillSheetCopy(copy.toasts.statusChanged, {
              key,
              status: label,
            }),
            emphasis: key,
            action: before
              ? {
                  label: copy.toasts.undo,
                  hint: copy.toasts.undoKey,
                  onAction: () =>
                    updateStatus.mutate({ cardId, statusOptionId: before }),
                }
              : undefined,
          });
        },
      },
    );
  };
  const patchItem = (cardId: string, patch: UpdateCardInput) => {
    const property: ItemPageProperty | null =
      patch.dueDate !== undefined
        ? "due"
        : patch.startDate !== undefined
          ? "startDate"
          : null;
    updateCard.mutate(
      { cardId, patch },
      {
        onError: (writeError) => {
          if (!property) return;
          refuse(
            writeError,
            { property, send: () => patchItem(cardId, patch) },
            property === "due" ? patch.dueDate : patch.startDate,
          );
        },
      },
    );
  };

  const lane = useMemo(() => {
    const lanes = groupCardsByStatus({
      fieldDefinitions: board.fieldDefinitions,
      groups: boardGroups,
    });
    return lanes.find((entry) =>
      entry.cards.some((entryCard) => entryCard.id === card.id),
    );
  }, [board.fieldDefinitions, boardGroups, card.id]);
  const laneCards = lane?.cards ?? [];
  const laneIndex = laneCards.findIndex((entry) => entry.id === card.id);
  const previousId = laneIndex > 0 ? laneCards[laneIndex - 1]?.id : undefined;
  const nextId = laneIndex >= 0 ? laneCards[laneIndex + 1]?.id : undefined;
  const position: ItemPagePosition | undefined =
    lane && laneIndex >= 0
      ? { index: laneIndex + 1, total: laneCards.length, laneName: lane.name }
      : undefined;

  const conflict = refused ? describeConflict(board, card, refused, now) : null;

  const pendingProperties = new Set<ItemPageProperty>();
  let pendingChildItems = false;
  for (const variables of paused) {
    if (typeof variables === "string") {
      pendingChildItems = true;
      continue;
    }
    if (typeof variables !== "object" || variables === null) continue;
    const write = variables as Record<string, unknown>;
    if ("childId" in write) pendingChildItems = true;
    else if (typeof write.statusOptionId === "string")
      pendingProperties.add("status");
    else if (typeof write.fieldDefinitionId === "string") {
      const property = propertyOf(write.fieldDefinitionId);
      if (property) pendingProperties.add(property);
    } else if (typeof write.patch === "object" && write.patch !== null) {
      const patch = write.patch as UpdateCardInput;
      if (patch.dueDate !== undefined) pendingProperties.add("due");
      if (patch.startDate !== undefined) pendingProperties.add("startDate");
    }
  }
  const offline = online
    ? null
    : {
        pendingCount: paused.length,
        properties: Array.from(pendingProperties),
        childItems: pendingChildItems,
      };

  const callbacks: TaskDetailCallbacks = {
    onClose: onBack,
    onRenameTitle: (cardId, patch) => updateCard.mutate({ cardId, patch }),
    onUpdateItem: patchItem,
    onToggleWatch: (watched) => follow.setFollowing(watched),
    onToggleChildItem: (childId, done) => children.toggle({ childId, done }),
    onAddChildItem: (title) => children.addChild(title),
    onAddLink: (itemId, relation) => links.addLink({ itemId, relation }),
    onRemoveLink: (linkId) => links.removeLink(linkId),
    onAddFile: startUpload,
    onDuplicate: (cardId) => duplicateCard.mutate(cardId),
    // "Lưu trữ" archives the item (it can be restored); it is not a delete.
    onArchive: (cardId) => {
      archiveCard.mutate(cardId);
      onBack();
    },
    onUpdateFieldValue: (payload) => {
      setRefused(null);
      // The Status pill writes through the status endpoint, like a drag.
      if (
        payload.fieldDefinitionId === fields.status?.id &&
        typeof payload.value === "string"
      )
        sendStatus(payload.cardId, payload.value);
      else updateFieldValue.mutate(payload);
    },
    onRefreshActivity: () => {
      void activity.refetch();
    },
    onCreateUpdate: (input, options) =>
      createUpdate.mutate(input, {
        onSuccess: options?.onSuccess,
        onSettled: options?.onSettled,
      }),
    onUpdateUpdate: (updateId, body) => updateUpdate.mutate({ updateId, body }),
    onDeleteUpdate: (updateId) => deleteUpdate.mutate(updateId),
    onSelectTab: () => undefined,
    onPrevious: previousId ? () => onOpenItem(previousId) : undefined,
    onNext: nextId ? () => onOpenItem(nextId) : undefined,
    onCopyLink,
  };

  const created = activity.data?.find((entry) => entry.type === "created");
  const linkCandidates = boardGroups
    .flatMap((group) => group.cards)
    .filter((entry) => entry.id !== card.id && !entry.parentItemId)
    .map((entry) => ({
      id: entry.id,
      key: entry.itemKey ?? null,
      title: entry.title,
      statusCategory: entry.statusCategory ?? null,
    }));

  return (
    <ItemPageSurface
      status="ready"
      board={board}
      card={card}
      capabilities={capabilities}
      callbacks={callbacks}
      navigation={navigation(position)}
      copy={copy}
      sheetCopy={sheetCopy}
      now={now}
      space={space}
      viewer={viewer}
      creatorName={created?.actor ?? null}
      access={
        access
          ? {
              level: access.myLevel,
              grantedBy: access.mySource.grantedBy?.name,
              expiresAt: access.mySource.expiresAt,
            }
          : null
      }
      conflict={conflict}
      offline={offline}
      uploads={uploads}
      onRetryUpload={(id) => {
        const file = queued.current.get(id);
        // A file over the limit stays refused; a broken transfer is sent again.
        if (file && file.size <= MAX_UPLOAD_BYTES) sendFile(file, id);
      }}
      onDismissUpload={(id) => {
        queued.current.delete(id);
        setUploads((list) => list.filter((entry) => entry.id !== id));
      }}
      people={{
        people: directory?.people ?? [],
        teams: directory?.teams ?? [],
        loading: directory === null,
        canInvite: directory?.canInvite === true,
        onInvite: (userId) => invitePerson(userId),
      }}
      lifecycle={{
        completed: isItemDone(card) ? (lifecycle?.completed ?? null) : null,
        archived: lifecycle?.archived ?? null,
        onReopen: () => reopen(),
        onRestore: () => restore(),
      }}
      onResolveConflict={(choice) => {
        const write = refused;
        setRefused(null);
        if (choice === "mine") write?.send();
      }}
      activeTab={activeTab}
      onSelectTab={onSelectTab}
      detailData={{
        files: files.data ?? card.files,
        filesLoading: files.isLoading,
        activity: activity.data ?? card.activity,
        activityLoading: activity.isLoading,
        activityFetching: activity.isFetching,
        updates: updates.data ?? card.updates,
        updatesLoading: updates.isLoading,
        childItems: children.data?.map((child) => ({
          ...child,
          itemKey: child.key,
        })),
        following: follow.following,
        followers: follow.followers.map((person) => ({
          userId: person.userId,
          name: person.name,
          initials: personInitials(person.name),
        })),
        links: links.data,
        linkCandidates,
      }}
    />
  );
}

/** A write the server refused with a conflict, and how to send it again. */
interface RefusedWrite {
  readonly property: ItemPageProperty;
  readonly fieldId?: string;
  readonly fieldName: string;
  /** The value the viewer chose, as the write carried it. */
  readonly mine: unknown;
  readonly send: () => void;
}

/**
 * The refused write as the banner words it: the field, the value the viewer
 * chose and the value now stored (the reads are refetched after a failure).
 */
function describeConflict(
  board: Board,
  card: CardDetail,
  refused: RefusedWrite,
  now: Date,
): ItemPageConflict {
  const fields = findItemFields(board);
  const field = board.fieldDefinitions.find(
    (entry) => entry.id === refused.fieldId,
  );
  const date = (value: unknown) =>
    typeof value === "string" ? (formatItemDate(value, now) ?? value) : "—";
  const show = (value: unknown): string => {
    if (value === null || value === undefined || value === "") return "—";
    if (refused.property === "due" || refused.property === "startDate")
      return date(value);
    if (field && field.options.length > 0 && typeof value === "string")
      return (
        field.options.find((option) => option.id === value)?.label ?? value
      );
    if (field && field.id === fields.person?.id && Array.isArray(value))
      return board.members
        .filter((member) => value.includes(member.userId))
        .map((member) => member.name)
        .join(", ");
    return String(value);
  };
  const stored =
    refused.property === "due"
      ? (card.dueAt ?? card.dueDate ?? null)
      : refused.property === "startDate"
        ? (card.startDate ?? null)
        : refused.property === "status"
          ? (card.statusOptionId ?? card.status)
          : refused.property === "assignee"
            ? card.members.map((member) => member.userId)
            : (card.fieldValues[refused.fieldId ?? ""] ?? null);
  return {
    property: refused.property,
    fieldName: refused.fieldName,
    mine: show(refused.mine),
    theirs: show(stored),
  };
}

function placeholderBoard(boardId: string, workspaceId: string): Board {
  const at = new Date(0).toISOString();
  return {
    id: boardId,
    workspaceId,
    title: "",
    background: { type: "color", value: "" },
    visibility: "workspace",
    isArchived: false,
    fieldDefinitions: [],
    members: [],
    createdAt: at,
    updatedAt: at,
  };
}

function emptyCallbacks(onBack: () => void): TaskDetailCallbacks {
  return {
    onClose: onBack,
    onRenameTitle: () => undefined,
    onToggleWatch: () => undefined,
    onDuplicate: () => undefined,
    onArchive: () => undefined,
    onUpdateFieldValue: () => undefined,
    onRefreshActivity: () => undefined,
    onCreateUpdate: () => undefined,
    onUpdateUpdate: () => undefined,
    onDeleteUpdate: () => undefined,
    onSelectTab: () => undefined,
  };
}
