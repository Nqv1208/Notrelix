import type { ApiRequestOptions, NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody, OperationResponse } from "@notrelix/contracts";
import type {
  ActivityLogResponseApi,
  AttachmentDtoApi,
  Card,
  CardDetail,
  CardDtoApi,
} from "@notrelix/work-management-core";
import type {
  CreateCardInput,
  UpdateCardInput,
  UpdateFieldValueInput,
  UploadCardFileInput,
} from "@notrelix/work-management-core";
import type { MoveCardInput } from "@notrelix/work-management-core";
import {
  mapActivityResponse,
  mapAttachmentDtoToCardFile,
  mapCardDto,
} from "@notrelix/work-management-core";

type CreateItemOp = "WorkManagement.BoardItems.Create";
type CreateItemBody = OperationRequestBody<CreateItemOp>;
type CreateItemResponse = OperationResponse<CreateItemOp, 200>;

type UpdateItemOp = "WorkManagement.BoardItems.Update";
type UpdateItemBody = OperationRequestBody<UpdateItemOp>;

type MoveItemOp = "WorkManagement.BoardItems.Move";
type MoveItemBody = OperationRequestBody<MoveItemOp>;

type UpdateFieldValueOp = "WorkManagement.BoardItems.UpdateFieldValue";
type UpdateFieldValueBody = OperationRequestBody<UpdateFieldValueOp>;

export function createCardApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getCard(cardId: string): Promise<CardDetail> {
      const card = await api.get<CardDtoApi>(endpoints.boardItems.detail(cardId));
      return mapCardDto(card);
    },

    async createCard(
      boardId: string,
      payload: CreateCardInput,
    ): Promise<void> {
      const body: CreateItemBody = {
        groupId: payload.listId, // canonical uses groupId!
        title: payload.title,
        position: payload.position,
      };
      await api.post<CreateItemResponse>(endpoints.boardItems.create(boardId), body, {
        headers: { "Idempotency-Key": crypto.randomUUID() },
      });
      // no longer returning getCard(id) because response is void.
    },

    async updateCard(cardId: string, patch: UpdateCardInput): Promise<void> {
      const body: UpdateItemBody = patch; // mapping fields
      await api.patch<void>(endpoints.boardItems.detail(cardId), body);
    },

    async deleteCard(cardId: string): Promise<void> {
      await api.delete<void>(endpoints.boardItems.detail(cardId));
    },

    async archiveCard(cardId: string): Promise<void> {
      await api.post<void>(endpoints.boardItems.archive(cardId));
    },

    async duplicateCard(cardId: string): Promise<void> {
      await api.post<void>(endpoints.boardItems.duplicate(cardId));
    },

    async moveCard(
      payload: MoveCardInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: MoveItemBody = {
        groupId: payload.listId, // mapping listId to groupId
        position: payload.position,
      };
      if (options) {
        await api.post<void>(
          endpoints.boardItems.move(payload.cardId),
          body,
          options,
        );
        return;
      }
      await api.post<void>(endpoints.boardItems.move(payload.cardId), body);
    },

    async updateFieldValue(payload: UpdateFieldValueInput): Promise<void> {
      const body: UpdateFieldValueBody = { value: payload.value };
      await api.patch<void>(endpoints.boardItems.fieldValue(payload.cardId, payload.fieldDefinitionId), body);
    },

    async getCardFiles(cardId: string) {
      const files = await api.get<AttachmentDtoApi[]>(
        endpoints.boardItems.attachments(cardId),
      );
      return files.map((file) => mapAttachmentDtoToCardFile(file, cardId));
    },

    async getCardActivity(cardId: string) {
      const activity = await api.get<ActivityLogResponseApi>(
        endpoints.boardItems.activity(cardId),
      );
      return mapActivityResponse(activity, cardId);
    },

    async uploadCardFile(input: UploadCardFileInput) {
      void input;
      throw new Error(
        "Card file upload requires a storage presign URL before attachment metadata can be registered.",
      );
    },
  };
}
