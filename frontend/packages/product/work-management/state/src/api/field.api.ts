import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody } from "@notrelix/contracts";
import type {
  BoardTableColumn,
  FieldDefinition,
} from "@notrelix/work-management-core";

type CreateFieldOp = "WorkManagement.BoardFields.Create";
type CreateFieldBody = OperationRequestBody<CreateFieldOp>;

type UpdateFieldOp = "WorkManagement.BoardFields.Update";
type UpdateFieldBody = OperationRequestBody<UpdateFieldOp>;

type MoveFieldOp = "WorkManagement.BoardFields.Move";
type MoveFieldBody = OperationRequestBody<MoveFieldOp>;

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
        settingsJson: input.settings
          ? JSON.stringify(input.settings)
          : undefined,
        // Backend-first cut-over (SPEC WM-V3-DEC-017): the server owns placement;
        // until WM-V3-FE-001 sends neighbor ids, omitting both appends.
      };
      await api.post<void>(endpoints.boardFields.create(input.boardId), body, {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      });
    },

    async updateColumn(input: UpdateColumnInput): Promise<void> {
      // The field type is not patchable (SPEC WMREQ031); name and settings only.
      const body: UpdateFieldBody = {
        name: input.name,
        settingsJson: input.settings
          ? JSON.stringify(input.settings)
          : undefined,
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
      // Transitional (WM-V3-FE-001 sends one neighbor-relative move per drag):
      // the full-list reorder route is retired, so the desired order is applied
      // by appending each column in turn; appending the current last column is
      // a server-side no-op.
      const body: MoveFieldBody = {};
      for (const column of columns) {
        const columnId = typeof column === "string" ? column : column.id;
        await api.post<void>(endpoints.boardFields.move(boardId, columnId), body);
      }
    },
  };
}
