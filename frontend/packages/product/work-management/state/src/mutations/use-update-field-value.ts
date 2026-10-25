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
import type { CardWriteHookOptions } from "./use-update-item-status";

interface FieldValueWriter {
  updateFieldValue(
    payload: UpdateFieldValueInput,
    options?: ApiRequestOptions & { readonly expectedVersion?: number },
  ): Promise<void>;
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
  const isDue =
    field?.fieldType === "date" ||
    payload.fieldDefinitionId.endsWith("field-due-date");
  const memberIds =
    field?.fieldType === "person" && Array.isArray(payload.value)
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
    send: (context) =>
      cards.updateFieldValue(payload, {
        ...context,
        expectedVersion:
          findBoardCard(board, payload.cardId)?.version ?? undefined,
      }),
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
