import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import type {
  FieldOption,
  FullBoardResponse,
  WorkStatusCategory,
} from "@notrelix/work-management-core";
import type { FieldOptionInput } from "../api/field-option.api";
import {
  applyAddedOption,
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
  ): Promise<void>;
  updateOption(
    input: FieldOptionInput & {
      boardId: string;
      fieldId: string;
      optionId: string;
      expectedVersion: number;
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
      (current) => restoreBoardField(current, target.fieldId, before),
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
    id: `optimistic-option-${crypto.randomUUID()}`,
    label: draft.name,
    color: draft.color,
    ...(draft.category ? { category: draft.category } : {}),
  };
  return runFieldOptionCommand(
    queryClient,
    target,
    (board) => applyAddedOption(board, target.fieldId, optimistic),
    (expectedVersion, options) =>
      writer.addOption(
        {
          boardId: target.boardId,
          fieldId: target.fieldId,
          name: draft.name,
          color: draft.color,
          category: draft.category,
          expectedVersion,
        },
        options,
      ),
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
