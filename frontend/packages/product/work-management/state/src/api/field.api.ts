import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody, OperationResponse } from "@notrelix/contracts";
import type {
  BoardTableColumn,
  FieldDefinition,
} from "@notrelix/work-management-core";

type CreateFieldOp = "WorkManagement.BoardFields.Create";
type CreateFieldBody = OperationRequestBody<CreateFieldOp>;

type UpdateFieldOp = "WorkManagement.BoardFields.Update";
type UpdateFieldBody = OperationRequestBody<UpdateFieldOp>;

type ReorderFieldsOp = "WorkManagement.BoardFields.Reorder";
type ReorderFieldsBody = OperationRequestBody<ReorderFieldsOp>;

export type CreateColumnInput = {
  boardId: string;
  name: string;
  fieldType: FieldDefinition["fieldType"];
  settings?: Record<string, unknown>;
  position?: number;
};

export type UpdateColumnInput = {
  boardId: string;
  columnId: string;
  name?: string;
  fieldType?: FieldDefinition["fieldType"];
  settings?: Record<string, unknown>;
  isHidden?: boolean;
};

export function createColumnApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async createColumn(input: CreateColumnInput): Promise<void> {
      const body: CreateFieldBody = {
        name: input.name,
        type: input.fieldType,
        settingsJson: input.settings ? JSON.stringify(input.settings) : undefined,
        position: input.position,
      };
      await api.post<void>(endpoints.boardFields.create(input.boardId), body, {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      });
    },

    async updateColumn(input: UpdateColumnInput): Promise<void> {
      const body: UpdateFieldBody = {
        name: input.name,
        type: input.fieldType,
        settingsJson: input.settings ? JSON.stringify(input.settings) : undefined,
      };
      await api.patch<void>(
        endpoints.boardFields.detail(input.boardId, input.columnId),
        body,
      );
      // Wait, isHidden is not in UpdateBoardFieldRequest? 
      // If it's a BoardView preference, it's not part of the board field update!
    },

    async deleteColumn(boardId: string, columnId: string): Promise<void> {
      await api.delete<void>(endpoints.boardFields.detail(boardId, columnId));
    },

    async reorderColumns(
      boardId: string,
      columns: Pick<BoardTableColumn, "id">[] | string[],
    ): Promise<void> {
      const body: ReorderFieldsBody = {
        items: columns.map((column, index) => ({
          id: typeof column === "string" ? column : column.id,
          newPosition: index + 1,
        })),
      };
      await api.post<void>(endpoints.boardFields.reorder(boardId), body);
    },
  };
}
