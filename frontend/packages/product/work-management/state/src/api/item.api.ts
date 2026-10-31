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

/** What a duplicate takes along besides name, dates and description. */
export interface DuplicateItemOptions {
  readonly name?: string;
  readonly includeFields?: boolean;
  readonly includeChecklists?: boolean;
  readonly includeSubItems?: boolean;
  readonly includeLabels?: boolean;
  readonly includeAssignees?: boolean;
}

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

type ArchiveItemOp = "WorkManagement.BoardItems.Archive";
type ArchiveItemBody = OperationRequestBody<ArchiveItemOp>;

type MoveItemOp = "WorkManagement.BoardItems.Move";
type MoveItemBody = OperationRequestBody<MoveItemOp>;
type MoveItemResponse = OperationResponse<MoveItemOp, 200>;

type UpdateFieldValueOp = "WorkManagement.BoardItems.UpdateFieldValue";
type UpdateFieldValueBody = OperationRequestBody<UpdateFieldValueOp>;
type UpdateFieldValueResponse = OperationResponse<UpdateFieldValueOp, 200>;

/** What a status or assignee write answers: the item's new version. */
type VersionAnswer = OperationResponse<"WorkManagement.BoardItems.UpdateStatus", 200>;

type BulkUpdateOp = "WorkManagement.BoardItems.BulkUpdate";
type BulkUpdateBody = OperationRequestBody<BulkUpdateOp>;
type BulkUpdateResponse = OperationResponse<BulkUpdateOp, 200>;

/** The one change a bulk request applies to every item it names. */
export type BulkItemChange =
  | { readonly type: "set-status"; readonly statusOptionId: string }
  | { readonly type: "set-due-date"; readonly dueDate: string }
  | { readonly type: "clear-due-date" }
  | { readonly type: "assign"; readonly userId: string }
  | { readonly type: "unassign-all" }
  | { readonly type: "archive" }
  | { readonly type: "set-priority"; readonly priorityOptionId: string }
  | { readonly type: "delete" };

type UpdateStatusOp = "WorkManagement.BoardItems.UpdateStatus";
type UpdateStatusBody = OperationRequestBody<UpdateStatusOp>;

export function createCardApi(client: NotrelixClient) {
  const api = client.api;
  // Every write names the item version the person saw (the caller takes it from the cache the screen renders
  // from); none reads a fresh one, so a colleague's change in between is refused (412), never overwritten.
  return {
    async getCard(cardId: string): Promise<CardDetail> {
      const card = await api.get<CardDtoApi>(
        endpoints.boardItems.detail(cardId),
      );
      return mapCardDto(card);
    },

    /** Creates the item and returns its id and first version (absent when the server sends none). */
    async createCard(
      boardId: string,
      payload: CreateCardInput,
      options?: ApiRequestOptions,
    ): Promise<{ id: string; version: number } | undefined> {
      const body: CreateItemBody = {
        groupId: payload.listId, // canonical uses groupId!
        title: payload.title,
        ...(payload.descriptionMd
          ? { descriptionMd: payload.descriptionMd }
          : {}),
        ...(payload.startDate ? { startDate: payload.startDate } : {}),
        ...(payload.dueDate ? { dueDate: payload.dueDate } : {}),
        // Backend-first cut-over (SPEC WM-V3-DEC-017): the server now owns placement
        // and expects neighbor ids; until WM-V3-FE-001 maps them, omitting both appends.
      };
      const created = await api.post<CreateItemResponse | null | undefined>(
        endpoints.boardItems.create(boardId),
        body,
        options ?? { headers: { "Idempotency-Key": crypto.randomUUID() } },
      );
      return created?.id
        ? { id: created.id, version: created.version ?? 1 }
        : undefined;
    },

    async updateCard(
      cardId: string,
      patch: UpdateCardInput,
      options: ApiRequestOptions & { readonly expectedVersion: number },
    ): Promise<void> {
      const { expectedVersion, ...request } = options;
      // A null date means "remove it": the server reads an omitted/null date as
      // "unchanged", so a removal is sent as its own flag.
      const { dueDate, startDate, ...rest } = patch;
      const body: UpdateItemBody = {
        ...rest,
        ...(dueDate === null
          ? { clearDueDate: true }
          : dueDate !== undefined
            ? { dueDate }
            : {}),
        ...(startDate === null
          ? { clearStartDate: true }
          : startDate !== undefined
            ? { startDate }
            : {}),
        expectedVersion,
      };
      await api.patch<void>(
        endpoints.boardItems.detail(cardId),
        body,
        request.idempotencyKey ? request : undefined,
      );
    },

    async deleteCard(cardId: string, expectedVersion: number): Promise<void> {
      await api.delete<void>(
        `${endpoints.boardItems.detail(cardId)}?expectedVersion=${expectedVersion}`,
      );
    },

    /**
     * `POST /board-items/{id}/archive` with the item version the person saw: a
     * stale version is refused (412) and nothing changes.
     */
    async archiveCard(
      cardId: string,
      expectedVersion: number,
      options?: ApiRequestOptions,
    ): Promise<{ version?: number }> {
      const body: ArchiveItemBody = { expectedVersion };
      const archived = await api.post<VersionAnswer | null | undefined>(
        endpoints.boardItems.archive(cardId),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
      return { version: archived?.version ?? undefined };
    },

    /**
     * `POST /board-items/{id}/duplicate`. Without options the copy takes name,
     * dates and description only; the options add what it takes along. Comments,
     * history, files and links are never copied. Returns the copy's id.
     */
    async duplicateCard(
      cardId: string,
      options?: DuplicateItemOptions,
    ): Promise<string | undefined> {
      const created = await api.post<{ id?: string } | string | null>(
        endpoints.boardItems.duplicate(cardId),
        options,
        { idempotencyKey: crypto.randomUUID() },
      );
      return typeof created === "string" ? created : (created?.id ?? undefined);
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
     * caller saw. Returns the item's new version for the next write of a chain.
     */
    async updateFieldValue(
      payload: UpdateFieldValueInput,
      options: ApiRequestOptions & { readonly expectedVersion: number },
    ): Promise<{ version?: number }> {
      const { expectedVersion, ...request } = options;
      const body: UpdateFieldValueBody = {
        value: payload.value,
        expectedVersion,
      };
      const written = await api.patch<UpdateFieldValueResponse | null | undefined>(
        endpoints.boardItems.fieldValue(
          payload.cardId,
          payload.fieldDefinitionId,
        ),
        body,
        request.idempotencyKey ? request : undefined,
      );
      return { version: written?.version ?? undefined };
    },

    /**
     * Makes `next` the item's assignees. The Assignee system field mirrors the
     * item's own members, so the backend refuses a field value for it: each
     * added person is `POST /board-items/{id}/assignees` and each removed one
     * `DELETE /board-items/{id}/assignees/{userId}`. The first change names the
     * version the person saw; each next one names the version the server
     * returned for the one before. Returns the item's last version.
     */
    async setAssignees(
      cardId: string,
      next: readonly string[],
      current: readonly string[],
      expectedVersion: number,
    ): Promise<number> {
      const wanted = new Set(next);
      const held = new Set(current);
      let version = expectedVersion;
      const advance = (written: VersionAnswer | null | undefined) => {
        if (typeof written?.version !== "number")
          throw new Error(
            `The server sent no version for item ${cardId} after an assignee change.`,
          );
        version = written.version;
      };
      for (const userId of current) {
        if (wanted.has(userId)) continue;
        advance(
          await api.delete<VersionAnswer | null | undefined>(
            `${endpoints.boardItems.assignee(cardId, userId)}?expectedVersion=${version}`,
            { idempotencyKey: crypto.randomUUID() },
          ),
        );
      }
      for (const userId of next) {
        if (held.has(userId)) continue;
        advance(
          await api.post<VersionAnswer | null | undefined>(
            endpoints.boardItems.assignees(cardId),
            { userId, expectedVersion: version },
            { idempotencyKey: crypto.randomUUID() },
          ),
        );
      }
      return version;
    },

    /**
     * One change for several items of a board as one request
     * (`POST /boards/{id}/items/bulk`): every item changes or none does, so a
     * refusal leaves the board as it was. Returns how many items changed.
     */
    async bulkUpdate(
      boardId: string,
      itemIds: readonly string[],
      change: BulkItemChange,
      expectedVersions: Readonly<Record<string, number>>,
      options?: ApiRequestOptions,
    ): Promise<number> {
      const body: BulkUpdateBody = {
        operation: change.type,
        itemIds: [...itemIds],
        expectedVersions: { ...expectedVersions },
        ...(change.type === "set-status"
          ? { status: change.statusOptionId }
          : {}),
        ...(change.type === "set-due-date" ? { dueDate: change.dueDate } : {}),
        ...(change.type === "assign" ? { assigneeUserId: change.userId } : {}),
        ...(change.type === "set-priority"
          ? { priority: change.priorityOptionId }
          : {}),
      };
      const result = await api.post<BulkUpdateResponse | null | undefined>(
        endpoints.boardItems.bulk(boardId),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
      return result?.updated ?? itemIds.length;
    },

    /**
     * Sets the item's Status option (`PATCH /board-items/{id}/status`). The
     * server resolves the board's Status field; the value is the option id.
     */
    async updateStatus(
      cardId: string,
      statusOptionId: string,
      expectedVersion: number,
      options?: ApiRequestOptions,
    ): Promise<{ version?: number }> {
      const body: UpdateStatusBody = {
        status: statusOptionId,
        expectedVersion,
      };
      const written = await api.patch<VersionAnswer | null | undefined>(
        endpoints.boardItems.status(cardId),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
      return { version: written?.version ?? undefined };
    },

    async getCardFiles(cardId: string) {
      const files = await api.get<AttachmentDtoApi[]>(
        endpoints.boardItems.attachments(cardId),
      );
      return files.map((file) => mapAttachmentDtoToCardFile(file, cardId));
    },

    async getCardActivity(cardId: string) {
      // The newest page of the history (the server pages newest first; 100 is its largest page).
      const activity = await api.get<ActivityLogResponseApi>(
        `${endpoints.boardItems.activity(cardId)}?page=1&pageSize=100`,
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

    /**
     * Attaches a web link: `POST /board-items/{id}/attachments` with the URL
     * (`CreateBoardItemAttachment`). One key per user action.
     */
    async createCardLink(
      cardId: string,
      link: { url: string; name: string },
      options: { idempotencyKey: string },
    ): Promise<void> {
      await api.post<void>(
        endpoints.boardItems.attachments(cardId),
        {
          filename: link.name,
          url: link.url,
          sizeBytes: 0,
          contentType: "text/uri-list",
          source: "link",
        },
        { idempotencyKey: options.idempotencyKey },
      );
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

    /** `DELETE /board-items/{id}/attachments/{attachmentId}`: removes a stored file. */
    async deleteCardFile(cardId: string, attachmentId: string): Promise<void> {
      await api.delete<void>(
        `${endpoints.boardItems.attachments(cardId)}/${attachmentId}`,
      );
    },
  };
}
