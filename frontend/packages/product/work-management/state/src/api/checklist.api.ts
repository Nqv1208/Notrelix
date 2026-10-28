import type { ApiRequestOptions, NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type { Checklist } from "@notrelix/work-management-core";
import { mapChecklists } from "./board-api-mappers";

type CreateChecklistOp = "WorkManagement.Checklists.Create";
type CreateChecklistBody = OperationRequestBody<CreateChecklistOp>;
type CreateChecklistResponse = OperationResponse<CreateChecklistOp, 200>;

type UpdateChecklistOp = "WorkManagement.Checklists.Update";
type UpdateChecklistBody = OperationRequestBody<UpdateChecklistOp>;

type CreateChecklistItemOp = "WorkManagement.Checklists.CreateItemByChecklist";
type CreateChecklistItemBody = OperationRequestBody<CreateChecklistItemOp>;
type CreateChecklistItemResponse = OperationResponse<
  CreateChecklistItemOp,
  200
>;

type UpdateChecklistItemOp = "WorkManagement.Checklists.UpdateItem";
type UpdateChecklistItemBody = OperationRequestBody<UpdateChecklistItemOp>;

type MoveChecklistOp = "WorkManagement.Checklists.Move";
type MoveChecklistBody = OperationRequestBody<MoveChecklistOp>;

type MoveChecklistItemOp = "WorkManagement.Checklists.MoveItem";
type MoveChecklistItemBody = OperationRequestBody<MoveChecklistItemOp>;

/** Where a list or item lands: between two siblings (either may be absent). */
export interface ChecklistPlacement {
  readonly previousSiblingId?: string | null;
  readonly nextSiblingId?: string | null;
}

export interface CreateChecklistInput extends ChecklistPlacement {
  cardId: string;
  title: string;
}

export interface UpdateChecklistInput {
  checklistId: string;
  title: string;
  expectedVersion: number;
}

export interface CreateChecklistItemInput extends ChecklistPlacement {
  checklistId: string;
  title: string;
  expectedVersion: number;
}

export interface UpdateChecklistItemInput {
  itemId: string;
  title: string;
  expectedVersion: number;
}

/** An item write names its checklist's version: the checklist is the consistency boundary. */
export interface ChecklistItemRef {
  itemId: string;
  expectedVersion: number;
}

export interface MoveChecklistInput extends ChecklistPlacement {
  checklistId: string;
  expectedVersion: number;
}

export interface MoveChecklistItemInput extends ChecklistPlacement {
  itemId: string;
  expectedVersion: number;
}

// Inline errors with Retry replace the global toast for every checklist write.
const quiet = (options?: ApiRequestOptions): ApiRequestOptions => ({
  ...options,
  skipGlobalErrorToast: true,
});

export function createChecklistApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getChecklists(cardId: string): Promise<Checklist[]> {
      return mapChecklists(
        await api.get<Parameters<typeof mapChecklists>[0]>(
          endpoints.boardItems.checklists(cardId),
        ),
      );
    },

    async createChecklist(
      input: CreateChecklistInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: CreateChecklistBody = {
        title: input.title,
        previousSiblingId: input.previousSiblingId ?? null,
        nextSiblingId: input.nextSiblingId ?? null,
      };
      await api.post<CreateChecklistResponse>(
        endpoints.boardItems.checklists(input.cardId),
        body,
        quiet(options),
      );
    },

    async updateChecklist(
      input: UpdateChecklistInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: UpdateChecklistBody = {
        title: input.title,
        expectedVersion: input.expectedVersion,
      };
      await api.patch<void>(
        endpoints.checklists.detail(input.checklistId),
        body,
        quiet(options),
      );
    },

    async deleteChecklist(
      checklistId: string,
      options?: ApiRequestOptions,
    ): Promise<void> {
      await api.delete<void>(
        endpoints.checklists.detail(checklistId),
        quiet(options),
      );
    },

    async moveChecklist(
      input: MoveChecklistInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: MoveChecklistBody = {
        expectedVersion: input.expectedVersion,
        previousSiblingId: input.previousSiblingId ?? null,
        nextSiblingId: input.nextSiblingId ?? null,
      };
      await api.post<void>(
        endpoints.checklists.move(input.checklistId),
        body,
        quiet(options),
      );
    },

    async createChecklistItem(
      input: CreateChecklistItemInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: CreateChecklistItemBody = {
        title: input.title,
        expectedVersion: input.expectedVersion,
        previousSiblingId: input.previousSiblingId ?? null,
        nextSiblingId: input.nextSiblingId ?? null,
      };
      await api.post<CreateChecklistItemResponse>(
        endpoints.checklists.items(input.checklistId),
        body,
        quiet(options),
      );
    },

    async renameChecklistItem(
      input: UpdateChecklistItemInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: UpdateChecklistItemBody = {
        title: input.title,
        expectedVersion: input.expectedVersion,
      };
      await api.patch<void>(
        endpoints.checklistItems.detail(input.itemId),
        body,
        quiet(options),
      );
    },

    /** Flips done / not done; the server owns the new state. */
    async toggleChecklistItem(
      input: ChecklistItemRef,
      options?: ApiRequestOptions,
    ): Promise<void> {
      await api.post<void>(
        `${endpoints.checklistItems.toggle(input.itemId)}?expectedVersion=${input.expectedVersion}`,
        undefined,
        quiet(options),
      );
    },

    async deleteChecklistItem(
      input: ChecklistItemRef,
      options?: ApiRequestOptions,
    ): Promise<void> {
      await api.delete<void>(
        `${endpoints.checklistItems.detail(input.itemId)}?expectedVersion=${input.expectedVersion}`,
        quiet(options),
      );
    },

    async moveChecklistItem(
      input: MoveChecklistItemInput,
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: MoveChecklistItemBody = {
        expectedVersion: input.expectedVersion,
        previousSiblingId: input.previousSiblingId ?? null,
        nextSiblingId: input.nextSiblingId ?? null,
      };
      await api.post<void>(
        endpoints.checklistItems.move(input.itemId),
        body,
        quiet(options),
      );
    },
  };
}
