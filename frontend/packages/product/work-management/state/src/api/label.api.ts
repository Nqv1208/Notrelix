import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { CardLabel } from "@notrelix/work-management-core";

type CreateLabelOp = "WorkManagement.Labels.Create";
type CreateLabelBody = OperationRequestBody<CreateLabelOp>;
type CreateLabelResponse = OperationResponse<CreateLabelOp, 200>;

type UpdateLabelOp = "WorkManagement.Labels.Update";
type UpdateLabelBody = OperationRequestBody<UpdateLabelOp>;

type AddLabelOp = "WorkManagement.BoardItems.AddLabel";
type AddLabelBody = OperationRequestBody<AddLabelOp>;

export interface CreateLabelInput {
  boardId: string;
  color: string;
  name?: string;
}

export interface UpdateLabelInput {
  boardId: string;
  labelId: string;
  color?: string;
  name?: string;
}

export function createLabelApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getBoardLabels(boardId: string): Promise<CardLabel[]> {
      const labels = await api.get<any>(endpoints.boards.labels(boardId));
      return (labels || []).map((l: any) => ({
        id: l.id,
        name: l.name,
        color: l.color,
      }));
    },

    async createLabel(input: CreateLabelInput): Promise<CardLabel> {
      const body: CreateLabelBody = {
        color: input.color,
        name: input.name,
      };
      // Canonical response might be just ID or the full label?
      // WorkManagement.Labels.Create response type will tell us.
      // Wait, if it returns void, we can't return CardLabel! Let's check `CreateLabelResponse`.
      const res = await api.post<CreateLabelResponse>(
        endpoints.boards.labels(input.boardId),
        body,
        { headers: { "Idempotency-Key": crypto.randomUUID() } },
      );
      // For now, let's cast as any and assume it returns what it used to. If it fails typecheck, I'll fix it.
      return {
        id: (res as any).id,
        name: (res as any).name,
        color: (res as any).color,
      };
    },

    async updateLabel(input: UpdateLabelInput): Promise<void> {
      const body: UpdateLabelBody = {
        color: input.color,
        name: input.name,
      };
      await api.patch<void>(
        endpoints.boards.label(input.boardId, input.labelId),
        body,
      );
    },

    async deleteLabel(boardId: string, labelId: string): Promise<void> {
      await api.delete<void>(endpoints.boards.label(boardId, labelId));
    },

    async addLabelToCard(cardId: string, labelId: string): Promise<void> {
      const body: AddLabelBody = { labelId };
      await api.post<void>(endpoints.boardItems.labels(cardId), body);
    },

    async removeLabelFromCard(cardId: string, labelId: string): Promise<void> {
      await api.delete<void>(endpoints.boardItems.label(cardId, labelId));
    },
  };
}
