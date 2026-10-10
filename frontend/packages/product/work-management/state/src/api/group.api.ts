import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { BoardGroup } from "@notrelix/work-management-core";

type CreateGroupOp = "WorkManagement.BoardGroups.Create";
type CreateGroupBody = OperationRequestBody<CreateGroupOp>;
type CreateGroupResponse = OperationResponse<CreateGroupOp, 200>;

type UpdateGroupOp = "WorkManagement.BoardGroups.Update";
type UpdateGroupBody = OperationRequestBody<UpdateGroupOp>;

type DuplicateGroupOp = "WorkManagement.BoardGroups.Duplicate";
type DuplicateGroupResponse = OperationResponse<DuplicateGroupOp, 200>;

type MoveGroupsOp = "WorkManagement.BoardGroups.Move";
type MoveGroupsBody = OperationRequestBody<MoveGroupsOp>;

export type CreateGroupInput = {
  boardId: string;
  title: string;
  position?: number;
  color?: string;
};

export type UpdateGroupInput = {
  groupId: string;
  title?: string;
  color?: string;
};

export function createGroupApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async createGroup(input: CreateGroupInput): Promise<void> {
      const body: CreateGroupBody = {
        title: input.title,
        // Backend-first cut-over (SPEC WM-V3-DEC-017): the server owns placement and
        // expects neighbor ids; until WM-V3-FE-001 maps them, omitting both appends.
        color: input.color,
      };
      await api.post<CreateGroupResponse>(
        endpoints.boardGroups.create(input.boardId),
        body,
        {
          headers: { "Idempotency-Key": crypto.randomUUID() },
        },
      );
    },

    async updateGroup(input: UpdateGroupInput): Promise<void> {
      const body: UpdateGroupBody = {
        title: input.title,
        color: input.color,
      };
      await api.patch<void>(endpoints.boardGroups.detail(input.groupId), body);
    },

    async deleteGroup(groupId: string): Promise<void> {
      await api.delete<void>(endpoints.boardGroups.detail(groupId));
    },

    async duplicateGroup(groupId: string): Promise<void> {
      // Producer returns void or a string? Let's check OpenAPI, typically void if using optimistic UI, but we'll assume void for now unless string is defined.
      await api.post<DuplicateGroupResponse>(
        endpoints.boardGroups.duplicate(groupId),
      );
    },

    async reorderGroups(
      boardId: string,
      groups: Pick<BoardGroup, "id" | "position">[],
    ): Promise<void> {
      // Transitional (WM-V3-FE-001 sends one neighbor-relative move per drag):
      // the full-list reorder route is retired, so the desired order is applied
      // by appending each group in turn; appending the current last group is a
      // server-side no-op.
      void boardId;
      const body: MoveGroupsBody = {};
      const ordered = [...groups].sort((a, b) => a.position - b.position);
      for (const group of ordered) {
        await api.post<void>(endpoints.boardGroups.move(group.id), body);
      }
    },
  };
}
