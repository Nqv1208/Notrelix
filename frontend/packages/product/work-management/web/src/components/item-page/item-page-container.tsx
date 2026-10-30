import { useEffect, useMemo, useState } from "react";
import {
  useCard,
  useCardActivity,
  useCardComments,
  useBoardLabels,
  useCardFiles,
  useDeleteCardFile,
  useCardLabelWrites,
  useChecklistWrites,
  useCreateCardUpdate,
  useDeleteCardUpdate,
  useToggleCommentReaction,
  useFullBoard,
  useItemChildItems,
  useItemApprovals,
  useItemFollowing,
  useItemFileUploads,
  canRetryItemFileUpload,
  useWorkManagementServices,
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
  MAX_ITEM_FILE_BYTES,
  personInitials,
  type Board,
  type CardDetail,
  type CardFile,
  type UpdateCardInput,
} from "@notrelix/work-management-core";
import { collaborationRefusalMessage } from "./collaboration-refusal";
import {
  useBoardCapabilities,
  useItemCapabilities,
} from "../shared/board-capabilities";
import { formatItemDate, nextLabelColor } from "../shared/item-display";
import { useItemSheetCopy } from "../card-detail/use-item-sheet-copy";
import type { ItemSheetCopy } from "../card-detail/item-sheet.copy";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import { findItemFields, isItemDone } from "../card-detail/item-field-editors";
import { withBoardPriority } from "../shared/item-priority";
import type { TaskDetailCallbacks } from "../card-detail/task-detail-types";
import { useItemPageCopy } from "./use-item-page-copy";
import { formatFileSize } from "./item-page-format";
import { useItemRemoval } from "../item-lifecycle/use-item-removal";
import { LinkedDocsSection } from "../item-actions/linked-docs-section";
import { saveBlobAs } from "./save-blob";
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
  const { access, capabilities: boardCapabilitiesOfViewer } =
    useBoardCapabilities(boardId, workspaceId);
  // An item inside an archived group stays readable and takes no write.
  const capabilities = useItemCapabilities(
    boardCapabilitiesOfViewer,
    boardId,
    workspaceId,
    card?.listId,
  );

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
  capabilities: ReturnType<typeof useBoardCapabilities>["capabilities"];
  access: ReturnType<typeof useBoardCapabilities>["access"];
  copy: ReturnType<typeof useItemPageCopy>;
  sheetCopy: ItemSheetCopy;
  now: Date;
  navigation: (position: ItemPagePosition | undefined) => ItemPageNavigation;
}) {
  // Files on their way: one row each, kept until sent, skipped or dismissed.
  const fileUploads = useItemFileUploads(card.id, card.workspaceId);
  const { cards: cardServices } = useWorkManagementServices();
  const files = useCardFiles(card.id, card.workspaceId);
  const deleteFile = useDeleteCardFile(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const boardLabels = useBoardLabels(card.boardId, card.workspaceId);
  const labelWrites = useCardLabelWrites(
    card.id,
    card.boardId,
    card.workspaceId,
  );
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);
  const toggleReaction = useToggleCommentReaction(card.id, card.workspaceId);
  const children = useItemChildItems(card.id, card.workspaceId, card.boardId);
  const checklistWrites = useChecklistWrites(
    card.id,
    card.boardId,
    card.workspaceId,
    card.checklists,
  );
  const follow = useItemFollowing(card.id, card.workspaceId);
  const approvals = useItemApprovals(card.id, card.workspaceId);
  const links = useItemLinks(card.id, card.workspaceId, card.boardId);
  const uploads: ItemPageUpload[] = fileUploads.uploads.map((upload) => ({
    ...upload,
    ...(upload.error === "tooBig"
      ? { limit: formatFileSize(MAX_ITEM_FILE_BYTES, sheetCopy.locale) }
      : {}),
    retryable: canRetryItemFileUpload(upload),
  }));
  /** A comment or file write the server refused: say why, the list is read again by the mutation. */
  const toastRefusal = (error: unknown) =>
    onToast?.({
      kind: "error",
      dismissLabel: copy.toasts.dismiss,
      title: collaborationRefusalMessage(error, copy.toasts),
    });
  /** Saves a stored file with the session: the content route takes no URL token. */
  const downloadFile = (file: CardFile) => {
    cardServices
      .downloadCardFile(card.id, file.id)
      .then((blob) => saveBlobAs(blob, file.name))
      .catch(() =>
        onToast?.({
          kind: "error",
          dismissLabel: copy.toasts.dismiss,
          title: fillSheetCopy(copy.files.downloadFailed, { name: file.name }),
        }),
      );
  };
  const { lifecycle, restore, reopen } = useItemLifecycle(
    card.id,
    card.workspaceId,
    card.boardId,
  );
  // Writes the network holds back: what the offline notice counts and marks.
  const { online, paused } = useOfflineWrites();
  const { directory } = usePeopleDirectory(card.boardId, card.workspaceId);

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

  const removal = useItemRemoval({
    workspaceId: card.workspaceId,
    boardId: card.boardId,
    titleOf: () => card.title,
    onGone: () => onBack(),
  });

  const callbacks: TaskDetailCallbacks = {
    onClose: onBack,
    onRenameTitle: (cardId, patch) => updateCard.mutate({ cardId, patch }),
    onUpdateItem: patchItem,
    onToggleLabel: (labelId, on) => labelWrites.mutate({ labelId, on }),
    onCreateLabel: (name, color) =>
      labelWrites
        .createAndApply(
          name,
          color ?? nextLabelColor(boardLabels.data?.length ?? 0),
        )
        .then(
          () => true,
          () => false,
        ),
    onToggleWatch: (watched) => follow.setFollowing(watched),
    onRequestApproval: (approverUserId) =>
      approvals.request({ approverUserId, note: null }),
    onDecideApproval: (requestId, decision, comment) =>
      approvals.decide({ requestId, decision, comment }),
    onCancelApproval: (requestId) => approvals.cancel(requestId),
    onToggleChildItem: (childId, done) => children.toggle({ childId, done }),
    onAddChildItem: (title) => children.addChild(title),
    onAddLink: (itemId, relation) => links.addLink({ itemId, relation }),
    onRemoveLink: (linkId) => links.removeLink(linkId),
    onAddFile: fileUploads.start,
    onDownloadFile: downloadFile,
    onDeleteFile: (file) =>
      deleteFile.mutateAsync(file.id).then(
        () => true,
        (error) => {
          toastRefusal(error);
          return false;
        },
      ),
    onDuplicate: capabilities.canCreateCard
      ? removal.requestDuplicate
      : () => undefined,
    // "Lưu trữ" asks first, then archives the item (it can be restored); it is not a delete.
    onArchive: removal.requestArchive,
    onUnarchive: removal.unarchiveItem,
    onComplete: removal.completeItem,
    onReopen: removal.reopenItem,
    onRemind: removal.requestRemind,
    onDelete: removal.requestDelete,
    onMoveToBoard: removal.requestMove,
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
    onToggleReaction: (commentId, emoji, on) =>
      toggleReaction.mutate({ commentId, emoji, on }),
    onUpdateUpdate: (updateId, body) =>
      updateUpdate.mutate({ updateId, body }, { onError: toastRefusal }),
    onDeleteUpdate: (updateId) =>
      deleteUpdate.mutate(updateId, { onError: toastRefusal }),
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
    <>
      <ItemPageSurface
        status="ready"
        board={board}
        card={card}
        capabilities={capabilities}
        callbacks={callbacks}
        linkedDocs={
          <LinkedDocsSection
            itemId={card.id}
            workspaceId={card.workspaceId}
            canEdit={capabilities.canEditFields}
          />
        }
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
              }
            : null
        }
        conflict={conflict}
        offline={offline}
        uploads={uploads}
        onRetryUpload={fileUploads.retry}
        onDismissUpload={fileUploads.dismiss}
        people={{
          people: directory?.people ?? [],
          teams: directory?.teams ?? [],
          loading: directory === null,
          canInvite: directory?.canInvite === true,
          onInvite: () => undefined,
        }}
        lifecycle={{
          completed: isItemDone(card) ? (lifecycle?.completed ?? null) : null,
          archived: lifecycle?.archived ?? null,
          onRestore: () => restore(),
          onReopen: () => reopen(),
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
          activityError: activity.isError && activity.data === undefined,
          updates: updates.data ?? card.updates,
          updatesLoading: updates.isLoading,
          checklists: {
            ...checklistWrites,
            checklists: card.checklists,
            canEdit: capabilities.canEditFields,
            canConvert: !card.parentItemId,
          },
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
          approvals: approvals.approvals?.map((approval) => ({
            id: approval.id,
            status: approval.status,
            approverUserId: approval.approverUserId,
            approverName:
              directory?.people.find(
                (person) => person.userId === approval.approverUserId,
              )?.name ??
              board.members.find(
                (member) => member.userId === approval.approverUserId,
              )?.name ??
              null,
            decisionNote: approval.decisionNote,
            requestedByUserId: approval.requestedByUserId,
          })),
          approvalAnswerError: approvals.answerError !== null,
          viewerUserId: viewer?.userId ?? null,
          approvalError: approvals.error
            ? isConflictError(approvals.error)
              ? "alreadyAsked"
              : "failed"
            : null,
          links: links.data,
          linkCandidates,
          boardLabels: boardLabels.data,
        }}
      />
      {removal.dialogs}
    </>
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
