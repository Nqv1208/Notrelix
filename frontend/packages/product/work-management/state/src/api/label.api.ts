import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { CardLabel } from "@notrelix/work-management-core";

type CreateLabelOp = "WorkManagement.Labels.Create";
type CreateLabelBody = OperationRequestBody<CreateLabelOp>;
type CreateLabelResponse = OperationResponse<CreateLabelOp, 201>;

type ListLabelsOp = "WorkManagement.Labels.List";
type ListLabelsResponse = OperationResponse<ListLabelsOp, 200>;

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
  /** The label version the person saw. */
  expectedVersion: number;
}

export function createLabelApi(client: NotrelixClient) {
  const api = client.api;
  // A label write names the version the person saw (the caller takes it from the list the panel renders).
  return {
    async getBoardLabels(boardId: string): Promise<CardLabel[]> {
      const labels = await api.get<ListLabelsResponse>(
        endpoints.boards.labels(boardId),
      );
      return (labels ?? []).flatMap((label) =>
        label.labelId
          ? [
              {
                id: label.labelId,
                name: label.name ?? "",
                color: label.color ?? "",
                ...(typeof label.version === "number"
                  ? { version: label.version }
                  : {}),
              },
            ]
          : [],
      );
    },

    async createLabel(input: CreateLabelInput): Promise<CardLabel> {
      const body: CreateLabelBody = {
        color: input.color,
        name: input.name,
      };
      const res = await api.post<CreateLabelResponse>(
        endpoints.boards.labels(input.boardId),
        body,
        { headers: { "Idempotency-Key": crypto.randomUUID() } },
      );
      // The server answers with the new label's id only.
      return { id: res, name: input.name ?? "", color: input.color };
    },

    async updateLabel(input: UpdateLabelInput): Promise<void> {
      const body: UpdateLabelBody = {
        color: input.color,
        name: input.name,
        expectedVersion: input.expectedVersion,
      };
      await api.patch<void>(endpoints.labels.detail(input.labelId), body);
    },

    async deleteLabel(labelId: string, expectedVersion: number): Promise<void> {
      await api.delete<void>(
        `${endpoints.labels.detail(labelId)}?expectedVersion=${expectedVersion}`,
      );
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
