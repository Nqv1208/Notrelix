import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { BoardGroup } from "@notrelix/work-management-core";

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
    async createList(input: CreateListInput): Promise<string> {
      return api.post<string>(endpoints.lists.byBoard(input.boardId), {
        title: input.title,
        position: input.position,
        color: input.color,
      });
    },

    async updateList(input: UpdateListInput): Promise<void> {
      await api.patch<void>(endpoints.lists.detail(input.listId), {
        title: input.title,
        color: input.color,
        isArchived: input.isArchived,
      });
    },

    async deleteList(listId: string): Promise<void> {
      await api.delete<void>(endpoints.lists.detail(listId));
    },

    async duplicateList(listId: string): Promise<string> {
      return api.post<string>(endpoints.lists.duplicate(listId));
    },

    async reorderLists(
      boardId: string,
      lists: Pick<BoardGroup, "id" | "position">[],
    ): Promise<void> {
      await api.post<void>(endpoints.lists.reorder(boardId), {
        items: lists.map((list) => ({
          id: list.id,
          newPosition: list.position,
        })),
      });
    },
  };
}
