import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody } from "@notrelix/contracts";
import type { FieldDefinition } from "@notrelix/work-management-core";

type CreateFieldOp = "WorkManagement.BoardFields.Create";
type CreateFieldBody = OperationRequestBody<CreateFieldOp>;

type UpdateFieldOp = "WorkManagement.BoardFields.Update";
type UpdateFieldBody = OperationRequestBody<UpdateFieldOp>;

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
    /** Creates the field and resolves with its id. */
    async createColumn(input: CreateColumnInput): Promise<string> {
      const body: CreateFieldBody = {
        name: input.name,
        // The domain enum is `MultiSelect` / `LongText`; the read model says `multi_select` / `long_text`.
        type: input.fieldType.replaceAll("_", ""),
        settingsJson: input.settings
          ? JSON.stringify(input.settings)
          : undefined,
        // Backend-first cut-over (SPEC WM-V3-DEC-017): the server owns placement;
        // until WM-V3-FE-001 sends neighbor ids, omitting both appends.
      };
      return api.post<string>(
        endpoints.boardFields.create(input.boardId),
        body,
        {
          headers: { "Idempotency-Key": crypto.randomUUID() },
        },
      );
    },

    /** The version the field has now; a field write names it so a stale one is refused. */
    async versionOf(boardId: string, columnId: string): Promise<number> {
      const schema = await api.get<{
        fields?: readonly { id: string; version?: number | null }[] | null;
      }>(`/boards/${boardId}/schema`);
      return schema?.fields?.find((field) => field.id === columnId)?.version ?? 0;
    },

    async updateColumn(input: UpdateColumnInput): Promise<void> {
      // The field type is not patchable (SPEC WMREQ031); name and settings only.
      const body: UpdateFieldBody = {
        name: input.name,
        settingsJson: input.settings
          ? JSON.stringify(input.settings)
          : undefined,
        expectedVersion: await this.versionOf(input.boardId, input.columnId),
      };
      await api.patch<void>(
        endpoints.boardFields.detail(input.boardId, input.columnId),
        body,
      );
      // Wait, isHidden is not in UpdateBoardFieldRequest?
      // If it's a BoardView preference, it's not part of the board field update!
    },

    async deleteColumn(boardId: string, columnId: string): Promise<void> {
      const version = await this.versionOf(boardId, columnId);
      await api.delete<void>(
        `${endpoints.boardFields.detail(boardId, columnId)}?expectedVersion=${version}`,
      );
    },
  };
}
