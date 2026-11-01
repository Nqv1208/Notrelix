import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import {
  OPTIMISTIC_OPTION_ID_PREFIX,
  orderByKey,
  type FieldDefinition,
  type FieldOption,
  type FullBoardResponse,
  type WorkStatusCategory,
} from "@notrelix/work-management-core";
import type {
  AddedFieldOption,
  FieldOptionInput,
} from "../api/field-option.api";
import {
  applyAddedOption,
  applyRemovedOption,
  applySwappedOptions,
  applyUpdatedOption,
  findBoardField,
  restoreBoardField,
} from "../cache/field-option";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** A Status or Select option as the user names it. */
export interface FieldOptionDraft {
  readonly name: string;
  readonly color: string;
  readonly category?: WorkStatusCategory;
}

export interface FieldOptionWriter {
  addOption(
    input: FieldOptionInput & {
      boardId: string;
      fieldId: string;
      expectedVersion: number;
    },
    options?: ApiRequestOptions,
  ): Promise<AddedFieldOption>;
  updateOption(
    input: FieldOptionInput & {
      boardId: string;
      fieldId: string;
      optionId: string;
      expectedVersion: number;
    },
    options?: ApiRequestOptions,
  ): Promise<void>;
  removeOption(
    input: {
      boardId: string;
      fieldId: string;
      optionId: string;
      expectedVersion: number;
      replacementOptionId?: string | null;
    },
    options?: ApiRequestOptions,
  ): Promise<void>;
  moveOption(
    input: {
      boardId: string;
      fieldId: string;
      optionId: string;
      expectedVersion: number;
      previousSiblingId?: string | null;
      nextSiblingId?: string | null;
    },
    options?: ApiRequestOptions,
  ): Promise<void>;
}

/** The option write was refused before it was sent. */
export class FieldOptionWriteError extends Error {
  constructor(readonly reason: "field-unknown" | "version-unknown") {
    super(
      reason === "field-unknown"
        ? "The field is not in the board read."
        : "The field's version is not known; reload the board.",
    );
    this.name = "FieldOptionWriteError";
  }
}

/** The field's options in board order. */
function orderedOptions(field: FieldDefinition | undefined): FieldOption[] {
  return orderByKey(field?.options ?? [], (option) => option.position);
}

interface FieldOptionTarget {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly fieldId: string;
}

/**
 * Runs one optimistic option write. The field takes the change at once; a
 * failure puts only this field back and rethrows, and the board read is
 * refetched either way (the server's option id and field version replace the
 * optimistic ones). The write needs the field's version from the board read:
 * without one it fails closed instead of sending a guess.
 */
async function runFieldOptionCommand(
  queryClient: QueryClient,
  target: FieldOptionTarget,
  change: (board: FullBoardResponse) => FullBoardResponse | undefined,
  send: (expectedVersion: number, options: ApiRequestOptions) => Promise<void>,
  kind: string,
  /** What a failure puts back: the field alone unless the change reached the cards. */
  restore: "field" | "board" = "field",
): Promise<void> {
  const boardKey = wmQueryKeys.fullBoard(target.workspaceId, target.boardId);
  await queryClient.cancelQueries({ queryKey: boardKey });
  const board = queryClient.getQueryData<FullBoardResponse>(boardKey);
  const before = findBoardField(board, target.fieldId);
  if (!before) throw new FieldOptionWriteError("field-unknown");
  if (!before.version || before.version <= 0) {
    throw new FieldOptionWriteError("version-unknown");
  }
  queryClient.setQueryData<FullBoardResponse | undefined>(
    boardKey,
    (current) => (current ? (change(current) ?? current) : current),
  );
  const id = `${kind}:${target.fieldId}:${crypto.randomUUID()}`;
  try {
    await send(before.version, { correlationId: id, idempotencyKey: id });
  } catch (error) {
    queryClient.setQueryData<FullBoardResponse | undefined>(
      boardKey,
      (current) =>
        restore === "board"
          ? (board ?? current)
          : restoreBoardField(current, target.fieldId, before),
    );
    throw error;
  } finally {
    await queryClient.invalidateQueries({ queryKey: boardKey });
  }
}

export function runAddFieldOption(
  queryClient: QueryClient,
  writer: FieldOptionWriter,
  target: FieldOptionTarget,
  draft: FieldOptionDraft,
): Promise<void> {
  const optimistic: FieldOption = {
    id: `${OPTIMISTIC_OPTION_ID_PREFIX}${crypto.randomUUID()}`,
    label: draft.name,
    color: draft.color,
    ...(draft.category ? { category: draft.category } : {}),
  };
  return runFieldOptionCommand(
    queryClient,
    target,
    (board) => applyAddedOption(board, target.fieldId, optimistic),
    (expectedVersion, options) =>
      writer
        .addOption(
          {
            boardId: target.boardId,
            fieldId: target.fieldId,
            name: draft.name,
            color: draft.color,
            category: draft.category,
            expectedVersion,
          },
          options,
        )
        .then(() => undefined),
    "add-field-option",
  );
}

export function runUpdateFieldOption(
  queryClient: QueryClient,
  writer: FieldOptionWriter,
  target: FieldOptionTarget & { readonly optionId: string },
  draft: FieldOptionDraft,
): Promise<void> {
  return runFieldOptionCommand(
    queryClient,
    target,
    (board) =>
      applyUpdatedOption(board, target.fieldId, target.optionId, {
        label: draft.name,
        color: draft.color,
        ...(draft.category ? { category: draft.category } : {}),
      }),
    (expectedVersion, options) =>
      writer.updateOption(
        {
          boardId: target.boardId,
          fieldId: target.fieldId,
          optionId: target.optionId,
          name: draft.name,
          color: draft.color,
          category: draft.category,
          expectedVersion,
        },
        options,
      ),
    "update-field-option",
  );
}

/**
 * Removes an option; the items holding it move to `replacementOptionId`
 * (required by a Status when items hold the option, which the producer
 * enforces). A failure puts the whole board read back: the change reached the
 * cards too.
 */
export function runRemoveFieldOption(
  queryClient: QueryClient,
  writer: FieldOptionWriter,
  target: FieldOptionTarget & { readonly optionId: string },
  replacementOptionId: string | null,
): Promise<void> {
  return runFieldOptionCommand(
    queryClient,
    target,
    (board) =>
      applyRemovedOption(
        board,
        target.fieldId,
        target.optionId,
        replacementOptionId,
      ),
    (expectedVersion, options) =>
      writer.removeOption(
        {
          boardId: target.boardId,
          fieldId: target.fieldId,
          optionId: target.optionId,
          expectedVersion,
          replacementOptionId,
        },
        options,
      ),
    "remove-field-option",
    "board",
  );
}

/**
 * Moves an option one place before or after its neighbour in the field's
 * order (the lane or group moves with it).
 */
export function runMoveFieldOption(
  queryClient: QueryClient,
  writer: FieldOptionWriter,
  target: FieldOptionTarget & { readonly optionId: string },
  direction: "before" | "after",
): Promise<void> {
  const board = queryClient.getQueryData<FullBoardResponse>(
    wmQueryKeys.fullBoard(target.workspaceId, target.boardId),
  );
  const options = orderedOptions(findBoardField(board, target.fieldId));
  const index = options.findIndex((option) => option.id === target.optionId);
  const neighbourIndex = direction === "before" ? index - 1 : index + 1;
  const neighbour = options[neighbourIndex];
  if (index < 0 || !neighbour) return Promise.resolve();
  // The option lands between the neighbour's other side and the neighbour.
  const previousSiblingId =
    direction === "before"
      ? (options[neighbourIndex - 1]?.id ?? null)
      : neighbour.id;
  const nextSiblingId =
    direction === "before"
      ? neighbour.id
      : (options[neighbourIndex + 1]?.id ?? null);
  return runFieldOptionCommand(
    queryClient,
    target,
    (current) =>
      applySwappedOptions(
        current,
        target.fieldId,
        target.optionId,
        neighbour.id,
      ),
    (expectedVersion, requestOptions) =>
      writer.moveOption(
        {
          boardId: target.boardId,
          fieldId: target.fieldId,
          optionId: target.optionId,
          expectedVersion,
          previousSiblingId,
          nextSiblingId,
        },
        requestOptions,
      ),
    "move-field-option",
  );
}

/**
 * Adds an option to a Status or Select field ("Thêm trạng thái"): optimistic,
 * rolled back on failure, the board read refetched afterwards.
 */
export function useAddFieldOption(
  boardId: string,
  workspaceId: string,
  options: { readonly onError?: (error: unknown) => void } = {},
) {
  const queryClient = useQueryClient();
  const { fieldOptions } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: FieldOptionDraft & { fieldId: string }) =>
      runAddFieldOption(
        queryClient,
        fieldOptions,
        { workspaceId, boardId, fieldId: input.fieldId },
        input,
      ),
    onError: options.onError,
  });
}

/** Renames, recolours or recategorises an option of a Status or Select field. */
export function useUpdateFieldOption(
  boardId: string,
  workspaceId: string,
  options: { readonly onError?: (error: unknown) => void } = {},
) {
  const queryClient = useQueryClient();
  const { fieldOptions } = useWorkManagementServices();
  return useMutation({
    mutationFn: (
      input: FieldOptionDraft & { fieldId: string; optionId: string },
    ) =>
      runUpdateFieldOption(
        queryClient,
        fieldOptions,
        {
          workspaceId,
          boardId,
          fieldId: input.fieldId,
          optionId: input.optionId,
        },
        input,
      ),
    onError: options.onError,
  });
}

/** Removes an option of a Status or Select field and re-homes its items. */
export function useRemoveFieldOption(
  boardId: string,
  workspaceId: string,
  options: { readonly onError?: (error: unknown) => void } = {},
) {
  const queryClient = useQueryClient();
  const { fieldOptions } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: {
      fieldId: string;
      optionId: string;
      replacementOptionId: string | null;
    }) =>
      runRemoveFieldOption(
        queryClient,
        fieldOptions,
        {
          workspaceId,
          boardId,
          fieldId: input.fieldId,
          optionId: input.optionId,
        },
        input.replacementOptionId,
      ),
    onError: options.onError,
  });
}

/** Moves an option one place; callers keep the control off while it runs. */
export function useMoveFieldOption(
  boardId: string,
  workspaceId: string,
  options: { readonly onError?: (error: unknown) => void } = {},
) {
  const queryClient = useQueryClient();
  const { fieldOptions } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: {
      fieldId: string;
      optionId: string;
      direction: "before" | "after";
    }) =>
      runMoveFieldOption(
        queryClient,
        fieldOptions,
        {
          workspaceId,
          boardId,
          fieldId: input.fieldId,
          optionId: input.optionId,
        },
        input.direction,
      ),
    onError: options.onError,
  });
}
