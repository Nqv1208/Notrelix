import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import {
  findPriorityField,
  findStatusField,
  priorityFromOptionName,
  updateCardInFullBoard,
  type BoardMember,
  type Card,
  type FieldDefinition,
  type FullBoardResponse,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import type { ApiRequestOptions } from "@notrelix/contracts";
import { findBoardCard } from "../cache/card-restore";
import { withItemStatus } from "../cache/item-status";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import { seenItemVersion } from "./seen-version";
import { UnknownItemVersionError } from "./use-move-card";
import type { CardWriteHookOptions } from "./use-update-item-status";

interface FieldValueWriter {
  updateFieldValue(
    payload: UpdateFieldValueInput,
    options: ApiRequestOptions & { readonly expectedVersion: number },
  ): Promise<unknown>;
  /** Writes the item's own assignee members (the Assignee system field). */
  setAssignees?(
    cardId: string,
    next: readonly string[],
    current: readonly string[],
    expectedVersion: number,
  ): Promise<unknown>;
}

/**
 * Formula and Rollup results of a card whose source value was just written:
 * kept as shown but `stale` until the board read comes back with the new result.
 */
function markComputedStale(
  computed: Card["computedValues"],
): Card["computedValues"] {
  if (!computed) return computed;
  return Object.fromEntries(
    Object.entries(computed).map(([id, value]) => [
      id,
      value.state === "ok" ? { state: "stale", value: value.value } : value,
    ]),
  );
}

/**
 * A card with one field value changed, keeping the card's derived fields in
 * step: Status (lane), priority word, due date and assignees.
 */
export function applyFieldValue<T extends Card>(
  card: T,
  payload: UpdateFieldValueInput,
  fields: readonly FieldDefinition[],
  boardMembers: readonly BoardMember[],
): T {
  const field = fields.find(
    (candidate) => candidate.id === payload.fieldDefinitionId,
  );
  const statusField = findStatusField(fields);
  const priorityField = findPriorityField(fields);
  const isStatus = field !== undefined && field.id === statusField?.id;
  const isPriority = field !== undefined && field.id === priorityField?.id;
  // Only the item's own Due (first Date field) and Assignee (first Person field)
  // move its due date and members; a custom Date or Person field is a plain value.
  const isDue =
    (field !== undefined &&
      field.id === fields.find((entry) => entry.fieldType === "date")?.id) ||
    payload.fieldDefinitionId.endsWith("field-due-date");
  const memberIds =
    field !== undefined &&
    field.id === fields.find((entry) => entry.fieldType === "person")?.id &&
    Array.isArray(payload.value)
      ? (payload.value as string[])
      : undefined;

  const base =
    isStatus && statusField && typeof payload.value === "string"
      ? withItemStatus(card, statusField, payload.value)
      : card;
  const priorityOption =
    isPriority && typeof payload.value === "string"
      ? priorityField?.options.find((option) => option.id === payload.value)
      : undefined;
  return {
    ...base,
    priority: isPriority
      ? priorityFromOptionName(priorityOption?.label)
      : base.priority,
    dueDate: isDue
      ? ((payload.value as string | null) ?? undefined)
      : base.dueDate,
    dueAt: isDue ? ((payload.value as string | null) ?? null) : base.dueAt,
    members: memberIds
      ? boardMembers
          .filter((member) => memberIds.includes(member.userId))
          .map((member) => ({
            id: `cm-${card.id}-${member.userId}`,
            userId: member.userId,
            name: member.name,
            initials: member.initials,
            avatarUrl: member.avatarUrl,
            color: member.color,
          }))
      : base.members,
    fieldValues: {
      ...base.fieldValues,
      [payload.fieldDefinitionId]: payload.value,
    },
    computedValues: markComputedStale(base.computedValues),
    updatedAt: new Date().toISOString(),
  };
}

/**
 * Writes one field value. The board read and the item detail (the sheet)
 * take the value at once; a failure puts only this card back and rethrows;
 * both reads are refetched either way.
 */
export function runFieldValueCommand(
  queryClient: QueryClient,
  cards: FieldValueWriter,
  {
    workspaceId,
    boardId,
    payload,
  }: { workspaceId: string; boardId: string; payload: UpdateFieldValueInput },
): Promise<void> {
  const board = queryClient.getQueryData<FullBoardResponse>(
    wmQueryKeys.fullBoard(workspaceId, boardId),
  );
  // The write names the version the person saw (the cache the screen renders from).
  const expectedVersion = seenItemVersion(queryClient, {
    workspaceId,
    boardId,
    cardId: payload.cardId,
  });
  if (expectedVersion === undefined) {
    return Promise.reject(new UnknownItemVersionError(payload.cardId));
  }
  const fields = board?.board.fieldDefinitions ?? [];
  const members = board?.board.members ?? [];
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId: payload.cardId,
    kind: "field-value",
    applyBoard: (current) =>
      updateCardInFullBoard(current, payload.cardId, (card) =>
        applyFieldValue(card, payload, fields, members),
      ) ?? current,
    applyDetail: (detail) => applyFieldValue(detail, payload, fields, members),
    send: (context) => {
      const field = fields.find(
        (candidate) => candidate.id === payload.fieldDefinitionId,
      );
      // The Assignee system field mirrors the item's members; the backend
      // writes it through the item's assignees, never as a field value.
      if (
        field?.isSystemField &&
        field.fieldType === "person" &&
        cards.setAssignees
      ) {
        const next = Array.isArray(payload.value)
          ? (payload.value as string[])
          : [];
        const current = (findBoardCard(board, payload.cardId)?.members ?? [])
          .map((member) => member.userId)
          .filter((id): id is string => typeof id === "string");
        return cards
          .setAssignees(payload.cardId, next, current, expectedVersion)
          .then(() => undefined);
      }
      // The server keeps a date field as an instant; the editors pick a day.
      const value =
        field?.fieldType === "date" &&
        typeof payload.value === "string" &&
        /^\d{4}-\d{2}-\d{2}$/.test(payload.value)
          ? `${payload.value}T00:00:00.000Z`
          : payload.value;
      return cards
        .updateFieldValue(
          { ...payload, value },
          { ...context, expectedVersion },
        )
        .then(() => undefined);
    },
  });
}

export function useUpdateFieldValue(
  boardId: string,
  workspaceId?: string,
  options: CardWriteHookOptions<UpdateFieldValueInput> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (payload: UpdateFieldValueInput) =>
      runFieldValueCommand(queryClient, cards, {
        workspaceId: workspaceId!,
        boardId,
        payload,
      }),
    onError: options.onError,
  });
}
