import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody, OperationResponse } from "@notrelix/contracts";
import type { ChecklistDtoApi } from "@notrelix/work-management-core";

type CreateChecklistOp = "WorkManagement.Checklists.Create";
type CreateChecklistBody = OperationRequestBody<CreateChecklistOp>;
type CreateChecklistResponse = OperationResponse<CreateChecklistOp, 200>;

type UpdateChecklistOp = "WorkManagement.Checklists.Update";
type UpdateChecklistBody = OperationRequestBody<UpdateChecklistOp>;

type DeleteChecklistOp = "WorkManagement.Checklists.Delete";

type CreateChecklistItemOp = "WorkManagement.Checklists.CreateItemByChecklist";
type CreateChecklistItemBody = OperationRequestBody<CreateChecklistItemOp>;
type CreateChecklistItemResponse = OperationResponse<CreateChecklistItemOp, 200>;

type UpdateChecklistItemOp = "WorkManagement.Checklists.UpdateItem";
type UpdateChecklistItemBody = OperationRequestBody<UpdateChecklistItemOp>;

type DeleteChecklistItemOp = "WorkManagement.Checklists.DeleteItem";

export interface CreateChecklistInput {
  cardId: string;
  title: string;
}

export interface UpdateChecklistInput {
  checklistId: string;
  title?: string;
  position?: number;
}

export interface CreateChecklistItemInput {
  checklistId: string;
  title: string;
}

export interface UpdateChecklistItemInput {
  itemId: string;
  title?: string;
  isChecked?: boolean;
  dueDate?: string | null;
  assigneeId?: string | null;
}

export function createChecklistApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getChecklists(cardId: string): Promise<ChecklistDtoApi[]> {
      return api.get<any>(endpoints.boardItems.checklists(cardId));
    },

    async createChecklist(input: CreateChecklistInput): Promise<void> {
      const body: CreateChecklistBody = {
        title: input.title,
      };
      await api.post<CreateChecklistResponse>(endpoints.boardItems.checklists(input.cardId), body, {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      });
    },

    async updateChecklist(input: UpdateChecklistInput): Promise<void> {
      const body: UpdateChecklistBody = {
        title: input.title,
        position: input.position,
      };
      await api.patch<void>(endpoints.checklists.detail(input.checklistId), body);
    },

    async deleteChecklist(checklistId: string): Promise<void> {
      await api.delete<void>(endpoints.checklists.detail(checklistId));
    },

    async createChecklistItem(
      input: CreateChecklistItemInput,
    ): Promise<void> {
      const body: CreateChecklistItemBody = {
        title: input.title,
      };
      await api.post<CreateChecklistItemResponse>(endpoints.checklists.items(input.checklistId), body, {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      });
    },

    async updateChecklistItem(input: UpdateChecklistItemInput): Promise<void> {
      const body: UpdateChecklistItemBody = {
        title: input.title,
        isChecked: input.isChecked,
        dueDate: input.dueDate,
        assigneeId: input.assigneeId,
      };
      await api.patch<void>(endpoints.checklistItems.detail(input.itemId), body);
    },

    async deleteChecklistItem(itemId: string): Promise<void> {
      await api.delete<void>(endpoints.checklistItems.detail(itemId));
    },
  };
}
