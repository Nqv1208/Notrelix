import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type {
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { findBoardField } from "../cache/field-option";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** A custom field as the item sheet asks for it; options only for Select and MultiSelect. */
export interface CreateItemFieldInput {
  readonly name: string;
  readonly fieldType: FieldDefinition["fieldType"];
  readonly options?: readonly {
    readonly name: string;
    readonly color: string;
  }[];
}

/**
 * Creates a custom field of a board and its first options. Not optimistic:
 * the first option write names the field's version from a fresh board read,
 * each next one the version the add before it answered; the board is read
 * again at the end. Resolves with the new field's id; throws when any step fails (the field may then exist
 * without all its options, and the board read shows what was kept).
 */
export function useCreateItemField(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { columns, fieldOptions } = useWorkManagementServices();

  return useCallback(
    async (input: CreateItemFieldInput): Promise<string> => {
      const boardKey = wmQueryKeys.fullBoard(workspaceId, boardId);
      const reread = async (fieldId: string) => {
        await queryClient.refetchQueries({ queryKey: boardKey });
        return findBoardField(
          queryClient.getQueryData<FullBoardResponse>(boardKey),
          fieldId,
        )?.version;
      };
      const fieldId = await columns.createColumn({
        boardId,
        name: input.name,
        fieldType: input.fieldType,
      });
      try {
        // The first add names the version of a fresh board read; each next one
        // names the version the add before it answered.
        let expectedVersion: number | undefined;
        for (const option of input.options ?? []) {
          expectedVersion ??= await reread(fieldId);
          if (!expectedVersion) throw new Error("field version unknown");
          const added = await fieldOptions.addOption({
            boardId,
            fieldId,
            name: option.name,
            color: option.color,
            expectedVersion,
          });
          expectedVersion = added.version;
        }
      } finally {
        await queryClient.refetchQueries({ queryKey: boardKey });
      }
      return fieldId;
    },
    [boardId, columns, fieldOptions, queryClient, workspaceId],
  );
}
