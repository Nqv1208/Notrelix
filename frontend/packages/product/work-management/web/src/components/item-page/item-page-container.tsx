import { useMemo, useState } from "react";
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
  useFullBoard,
  useUpdateCard,
  useUpdateCardUpdate,
  useUpdateFieldValue,
  useUpdateItemStatus,
} from "@notrelix/work-management-state";
import {
  groupCardsByStatus,
  type Board,
  type CardDetail,
  type UpdateCardInput,
} from "@notrelix/work-management-core";
import { boardCapabilities } from "../shared/board-capabilities";
import { formatItemDate } from "../shared/item-display";
import { useItemSheetCopy } from "../card-detail/use-item-sheet-copy";
import type { ItemSheetCopy } from "../card-detail/item-sheet.copy";
import { findItemFields } from "../card-detail/item-field-editors";
import type { TaskDetailCallbacks } from "../card-detail/task-detail-types";
import { useItemPageCopy } from "./use-item-page-copy";
import { ItemPageSurface } from "./item-page-surface";
import type {
  ItemPageConflict,
  ItemPageNavigation,
  ItemPagePerson,
  ItemPageProperty,
  ItemPagePosition,
  ItemPageSpace,
  ItemPageTab,
  ItemPageViewKind,
} from "./item-page-types";

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
  const missing =
    Boolean(error) ||
    (card !== undefined &&
      (card.boardId !== boardId || card.workspaceId !== workspaceId));

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
      />
    );
  if (missing || !card)
    return (
      <ItemPageSurface
        status="error"
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
      card={card}
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
  const files = useCardFiles(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const archiveCard = useArchiveCard(card.boardId, card.workspaceId);
  const duplicateCard = useDuplicateCard(card.boardId, card.workspaceId);
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);

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
    if (!isConflictError(error)) return;
    const fieldName =
      write.property === "startDate"
        ? copy.rail.startDate
        : write.property === "due" && !fields.due
          ? copy.rail.due
          : (board.fieldDefinitions.find((field) => field.id === write.fieldId)
              ?.name ?? "");
    setRefused({ ...write, fieldName, mine });
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

  const callbacks: TaskDetailCallbacks = {
    onClose: onBack,
    onRenameTitle: (cardId, patch) => updateCard.mutate({ cardId, patch }),
    onUpdateItem: patchItem,
    onToggleWatch: () => undefined,
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
        updateStatus.mutate({
          cardId: payload.cardId,
          statusOptionId: payload.value,
        });
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
            }
          : null
      }
      conflict={conflict}
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
