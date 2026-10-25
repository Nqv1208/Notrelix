import type {
  ApiRequestOptions,
  NotrelixClient,
  UploadProgress,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type {
  ActivityLogResponseApi,
  AttachmentDtoApi,
  CardDetail,
  CardDtoApi,
  CardFile,
} from "@notrelix/work-management-core";
import type {
  CreateCardInput,
  UpdateCardInput,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import {
  mapActivityResponse,
  mapAttachmentDtoToCardFile,
  mapCardDto,
} from "./board-api-mappers";

/** How one user action sends a file (see `uploadCardFile`). */
export interface UploadCardFileOptions {
  readonly idempotencyKey: string;
  readonly signal?: AbortSignal;
  readonly onProgress?: (progress: UploadProgress) => void;
}

type CreateItemOp = "WorkManagement.BoardItems.Create";
type CreateItemBody = OperationRequestBody<CreateItemOp>;
type CreateItemResponse = OperationResponse<CreateItemOp, 200>;

type UpdateItemOp = "WorkManagement.BoardItems.Update";
type UpdateItemBody = OperationRequestBody<UpdateItemOp>;

type MoveItemOp = "WorkManagement.BoardItems.Move";
type MoveItemBody = OperationRequestBody<MoveItemOp>;
type MoveItemResponse = OperationResponse<MoveItemOp, 200>;

type UpdateFieldValueOp = "WorkManagement.BoardItems.UpdateFieldValue";
type UpdateFieldValueBody = OperationRequestBody<UpdateFieldValueOp>;

type UpdateStatusOp = "WorkManagement.BoardItems.UpdateStatus";
type UpdateStatusBody = OperationRequestBody<UpdateStatusOp>;

export function createCardApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getCard(cardId: string): Promise<CardDetail> {
      const card = await api.get<CardDtoApi>(
        endpoints.boardItems.detail(cardId),
      );
      return mapCardDto(card);
    },

    /** Creates the item and returns its id (absent when the server sends none). */
    async createCard(
      boardId: string,
      payload: CreateCardInput,
      options?: ApiRequestOptions,
    ): Promise<string | undefined> {
      const body: CreateItemBody = {
        groupId: payload.listId, // canonical uses groupId!
        title: payload.title,
        // Backend-first cut-over (SPEC WM-V3-DEC-017): the server now owns placement
        // and expects neighbor ids; until WM-V3-FE-001 maps them, omitting both appends.
      };
      const created = await api.post<CreateItemResponse | null | undefined>(
        endpoints.boardItems.create(boardId),
        body,
        options ?? { headers: { "Idempotency-Key": crypto.randomUUID() } },
      );
      return created?.id ?? undefined;
    },

    async updateCard(
      cardId: string,
      patch: UpdateCardInput,
      options?: ApiRequestOptions & { readonly expectedVersion?: number },
    ): Promise<void> {
      const { expectedVersion: known, ...request } = options ?? {};
      const expectedVersion =
        known ?? (await this.getCard(cardId)).version ?? undefined;
      const body: UpdateItemBody = { ...patch, expectedVersion }; // mapping fields
      await api.patch<void>(
        endpoints.boardItems.detail(cardId),
        body,
        request.idempotencyKey ? request : undefined,
      );
    },

    async deleteCard(cardId: string): Promise<void> {
      await api.delete<void>(endpoints.boardItems.detail(cardId));
    },

    async archiveCard(
      cardId: string,
      options?: ApiRequestOptions,
    ): Promise<void> {
      await api.post<void>(
        endpoints.boardItems.archive(cardId),
        undefined,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
    },

    async duplicateCard(cardId: string): Promise<void> {
      await api.post<void>(endpoints.boardItems.duplicate(cardId));
    },

    /**
     * `POST /board-items/{id}/move`: the target group, the item version the
     * move is based on and the new neighbours (both `null` appends).
     * Returns the item's new version.
     */
    async moveCard(
      move: {
        cardId: string;
        listId: string;
        expectedVersion: number;
        previousSiblingId: string | null;
        nextSiblingId: string | null;
      },
      options?: ApiRequestOptions,
    ): Promise<{ version?: number }> {
      const body: MoveItemBody = {
        groupId: move.listId,
        expectedVersion: move.expectedVersion,
        previousSiblingId: move.previousSiblingId,
        nextSiblingId: move.nextSiblingId,
      };
      const moved = await api.post<MoveItemResponse | null | undefined>(
        endpoints.boardItems.move(move.cardId),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
      return { version: moved?.version ?? undefined };
    },

    /**
     * `PATCH /board-items/{id}/values/{fieldId}`, based on the item version the
     * caller last read; without one the item is read first so the write never
     * runs on a guessed version.
     */
    async updateFieldValue(
      payload: UpdateFieldValueInput,
      options?: ApiRequestOptions & { readonly expectedVersion?: number },
    ): Promise<void> {
      const { expectedVersion: known, ...request } = options ?? {};
      const expectedVersion =
        known ?? (await this.getCard(payload.cardId)).version ?? undefined;
      const body: UpdateFieldValueBody = {
        value: payload.value,
        expectedVersion,
      };
      await api.patch<void>(
        endpoints.boardItems.fieldValue(
          payload.cardId,
          payload.fieldDefinitionId,
        ),
        body,
        request.idempotencyKey ? request : undefined,
      );
    },

    /**
     * Sets the item's Status option (`PATCH /board-items/{id}/status`). The
     * server resolves the board's Status field; the value is the option id.
     */
    async updateStatus(
      cardId: string,
      statusOptionId: string,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: UpdateStatusBody = { status: statusOptionId };
      await api.patch<void>(
        endpoints.boardItems.status(cardId),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
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

    /**
     * Sends a file to an item: `POST /board-items/{id}/attachments/files`
     * (multipart, part `file`). `idempotencyKey` belongs to one user action:
     * a fresh key per action, the same one only to retry that action.
     */
    async uploadCardFile(
      cardId: string,
      file: File,
      options: UploadCardFileOptions,
    ): Promise<CardFile> {
      const form = new FormData();
      form.append("file", file, file.name);
      const dto = await api.upload<AttachmentDtoApi>(
        endpoints.boardItems.attachmentFiles(cardId),
        form,
        {
          idempotencyKey: options.idempotencyKey,
          ...(options.signal ? { signal: options.signal } : {}),
          ...(options.onProgress ? { onProgress: options.onProgress } : {}),
        },
      );
      return mapAttachmentDtoToCardFile(dto, cardId);
    },

    /** The bytes of a stored file, read with the session (never a URL token). */
    downloadCardFile(
      cardId: string,
      attachmentId: string,
      options?: ApiRequestOptions,
    ): Promise<Blob> {
      return api.getBlob(
        endpoints.boardItems.attachmentContent(cardId, attachmentId),
        options,
      );
    },
  };
}
