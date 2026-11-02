import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import {
  updateCardInFullBoard,
  type Card,
  type CardDetail,
  type FullBoardResponse,
  type UpdateCardInput,
} from "@notrelix/work-management-core";
import type { ApiRequestOptions } from "@notrelix/contracts";
import { findBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { outcomeUnknown, runCardCommand } from "./card-command";
import { UnknownItemVersionError } from "./use-move-card";

interface CardUpdater {
  updateCard(
    cardId: string,
    patch: UpdateCardInput,
    options?: ApiRequestOptions & { readonly expectedVersion?: number },
  ): Promise<void>;
}

/** An edit sent without an answer: a repeat of it must send the same key and version. */
interface UnknownOutcome {
  readonly signature: string;
  readonly idempotencyKey: string;
  readonly expectedVersion: number;
}

const unknownByClient = new WeakMap<QueryClient, Map<string, UnknownOutcome>>();

function unknownOutcomes(
  queryClient: QueryClient,
): Map<string, UnknownOutcome> {
  let unknown = unknownByClient.get(queryClient);
  if (!unknown) {
    unknown = new Map();
    unknownByClient.set(queryClient, unknown);
  }
  return unknown;
}

/** A card with the patch applied (title, description, priority, dates). */
export function applyCardPatch<T extends Card>(
  card: T,
  patch: UpdateCardInput,
): T {
  return {
    ...card,
    title: patch.title ?? card.title,
    descriptionMd: patch.descriptionMd ?? card.descriptionMd,
    priority:
      patch.priority === null ? undefined : (patch.priority ?? card.priority),
    dueDate:
      patch.dueDate === null ? undefined : (patch.dueDate ?? card.dueDate),
    // The board read carries the due date as `dueAt`; keep it in step.
    dueAt: patch.dueDate === undefined ? card.dueAt : patch.dueDate,
    startDate:
      patch.startDate === null
        ? undefined
        : (patch.startDate ?? card.startDate),
    fieldValues: {
      ...card.fieldValues,
      [`${card.boardId}-field-title`]:
        patch.title ?? card.fieldValues[`${card.boardId}-field-title`],
      [`${card.boardId}-field-due-date`]:
        patch.dueDate ?? card.fieldValues[`${card.boardId}-field-due-date`],
      [`${card.boardId}-field-priority`]:
        patch.priority ?? card.fieldValues[`${card.boardId}-field-priority`],
    },
    updatedAt: new Date().toISOString(),
  };
}

/**
 * Updates an item (title and other item fields). The board read and the item
 * detail take the change at once; a failure puts only this card back.
 */
export function runUpdateCardCommand(
  queryClient: QueryClient,
  cards: CardUpdater,
  {
    workspaceId,
    boardId,
    cardId,
    patch,
  }: {
    workspaceId: string;
    boardId: string;
    cardId: string;
    patch: UpdateCardInput;
  },
): Promise<void> {
  // A repeat of an edit whose answer never came is the same operation: it keeps
  // its idempotency key and version, so the server replays the first result
  // instead of refusing the moved version as stale.
  const signature = JSON.stringify(patch);
  const unknown = unknownOutcomes(queryClient);
  const kept = unknown.get(cardId);
  // The write is based on the item version the member last read.
  const expectedVersion =
    kept?.signature === signature
      ? kept.expectedVersion
      : (
          findBoardCard(
            queryClient.getQueryData<FullBoardResponse>(
              wmQueryKeys.fullBoard(workspaceId, boardId),
            ),
            cardId,
          ) ??
          queryClient.getQueryData<CardDetail>(
            wmQueryKeys.cardDetail(workspaceId, cardId),
          )
        )?.version;
  if (!expectedVersion || expectedVersion <= 0) {
    return Promise.reject(new UnknownItemVersionError(cardId));
  }
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId,
    kind: "update-item",
    applyBoard: (current) =>
      updateCardInFullBoard(current, cardId, (card) =>
        applyCardPatch(card, patch),
      ) ?? current,
    applyDetail: (detail) => applyCardPatch(detail, patch),
    send: (context) => {
      const idempotencyKey =
        kept?.signature === signature
          ? kept.idempotencyKey
          : context.idempotencyKey;
      return cards
        .updateCard(cardId, patch, {
          ...context,
          idempotencyKey,
          expectedVersion,
        })
        .then(
          () => {
            unknown.delete(cardId);
          },
          (error: unknown) => {
            if (outcomeUnknown(error))
              unknown.set(cardId, {
                signature,
                idempotencyKey,
                expectedVersion,
              });
            else unknown.delete(cardId);
            throw error;
          },
        );
    },
  });
}

export function useUpdateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: ({
      cardId,
      patch,
    }: {
      cardId: string;
      patch: UpdateCardInput;
    }) =>
      runUpdateCardCommand(queryClient, cards, {
        workspaceId: workspaceId!,
        boardId,
        cardId,
        patch,
      }),
  });
}
