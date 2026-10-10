import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { BoardGroup } from "@notrelix/work-management-core";

type CreateListOp = "WorkManagement.BoardGroups.Create";
type CreateListBody = OperationRequestBody<CreateListOp>;
type CreateListResponse = OperationResponse<CreateListOp, 200>;

type UpdateListOp = "WorkManagement.BoardGroups.Update";
type UpdateListBody = OperationRequestBody<UpdateListOp>;

type DuplicateListOp = "WorkManagement.BoardGroups.Duplicate";
type DuplicateListResponse = OperationResponse<DuplicateListOp, 200>;

type MoveListsOp = "WorkManagement.BoardGroups.Move";
type MoveListsBody = OperationRequestBody<MoveListsOp>;

export interface CreateListInput {
  boardId: string;
  title: string;
  position?: number;
  color?: string;
}

export interface UpdateListInput {
  listId: string;
  title?: string;
  color?: string;
  isArchived?: boolean;
}

export function createListApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async createList(input: CreateListInput): Promise<void> {
      const body: CreateListBody = {
        title: input.title,
        // Backend-first cut-over (SPEC WM-V3-DEC-017): the server owns placement and
        // expects neighbor ids; until WM-V3-FE-001 maps them, omitting both appends.
        color: input.color,
      };
      await api.post<CreateListResponse>(
        endpoints.boardGroups.create(input.boardId),
        body,
        {
          headers: { "Idempotency-Key": crypto.randomUUID() },
        },
      );
    },

    async updateList(input: UpdateListInput): Promise<void> {
      const body: UpdateListBody = {
        title: input.title,
        color: input.color,
      };
      await api.patch<void>(endpoints.boardGroups.detail(input.listId), body);
    },

    async deleteList(listId: string): Promise<void> {
      await api.delete<void>(endpoints.boardGroups.detail(listId));
    },

    async duplicateList(listId: string): Promise<void> {
      await api.post<DuplicateListResponse>(
        endpoints.boardGroups.duplicate(listId),
      );
    },

    async reorderLists(
      boardId: string,
      lists: Pick<BoardGroup, "id" | "position">[],
    ): Promise<void> {
      // Transitional (WM-V3-FE-001 sends one neighbor-relative move per drag):
      // the full-list reorder route is retired, so the desired order is applied
      // by appending each list in turn; appending the current last list is a
      // server-side no-op.
      void boardId;
      const body: MoveListsBody = {};
      const ordered = [...lists].sort((a, b) => a.position - b.position);
      for (const list of ordered) {
        await api.post<void>(endpoints.boardGroups.move(list.id), body);
      }
    },
  };
}
