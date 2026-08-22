import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody, OperationResponse } from "@notrelix/contracts";
import type { BoardGroup } from "@notrelix/work-management-core";

type CreateGroupOp = "WorkManagement.BoardGroups.Create";
type CreateGroupBody = OperationRequestBody<CreateGroupOp>;
type CreateGroupResponse = OperationResponse<CreateGroupOp, 200>;

type UpdateGroupOp = "WorkManagement.BoardGroups.Update";
type UpdateGroupBody = OperationRequestBody<UpdateGroupOp>;

type DeleteGroupOp = "WorkManagement.BoardGroups.Delete";

type DuplicateGroupOp = "WorkManagement.BoardGroups.Duplicate";
type DuplicateGroupResponse = OperationResponse<DuplicateGroupOp, 200>;

type ReorderGroupsOp = "WorkManagement.BoardGroups.Reorder";
type ReorderGroupsBody = OperationRequestBody<ReorderGroupsOp>;

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
        position: input.position,
        color: input.color,
      };
      await api.post<CreateGroupResponse>(endpoints.boardGroups.create(input.boardId), body, {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      });
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
      await api.post<DuplicateGroupResponse>(endpoints.boardGroups.duplicate(groupId));
    },

    async reorderGroups(
      boardId: string,
      groups: Pick<BoardGroup, "id" | "position">[],
    ): Promise<void> {
      const body: ReorderGroupsBody = {
        items: groups.map((group) => ({
          id: group.id,
          newPosition: group.position,
        })),
      };
      await api.post<void>(endpoints.boardGroups.reorder(boardId), body);
    },
  };
}
