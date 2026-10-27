import type {
  ApiRequestOptions,
  NotrelixClient,
  OperationRequestBody,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { WorkStatusCategory } from "@notrelix/work-management-core";

type AddFieldOptionBody =
  OperationRequestBody<"WorkManagement.BoardFields.AddFieldOption">;
type UpdateFieldOptionBody =
  OperationRequestBody<"WorkManagement.BoardFields.UpdateFieldOption">;
type MoveFieldOptionBody =
  OperationRequestBody<"WorkManagement.BoardFields.MoveFieldOption">;

/** An option of a Status or Select field as a user names it. */
export interface FieldOptionInput {
  readonly name: string;
  /** The stored colour (`#rrggbb`). */
  readonly color: string;
  /** Status options only: where the option sits in the work lifecycle. */
  readonly category?: WorkStatusCategory;
}

export function createFieldOptionApi(client: NotrelixClient) {
  const api = client.api;
  return {
    /**
     * `POST /boards/{boardId}/fields/{fieldId}/options`: appends an option
     * (no neighbours named). `expectedVersion` is the field's version from the
     * board read; a stale one is a precondition failure, not an overwrite.
     */
    async addOption(
      input: FieldOptionInput & {
        boardId: string;
        fieldId: string;
        expectedVersion: number;
      },
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: AddFieldOptionBody = {
        name: input.name,
        color: input.color,
        category: input.category ?? null,
        expectedVersion: input.expectedVersion,
      };
      await api.post<unknown>(
        endpoints.boardFields.options(input.boardId, input.fieldId),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
    },

    /** `PATCH .../options/{optionId}`: renames, recolours or recategorises. */
    async updateOption(
      input: FieldOptionInput & {
        boardId: string;
        fieldId: string;
        optionId: string;
        expectedVersion: number;
      },
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: UpdateFieldOptionBody = {
        name: input.name,
        color: input.color,
        category: input.category ?? null,
        expectedVersion: input.expectedVersion,
      };
      await api.patch<void>(
        endpoints.boardFields.option(
          input.boardId,
          input.fieldId,
          input.optionId,
        ),
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
    },

    /**
     * `DELETE .../options/{optionId}?expectedVersion=&replacementOptionId=`:
     * removes the option and moves the items that hold it to the replacement
     * (a Status needs one when any item holds the option; a Select without one
     * clears the value). The producer refuses the last Not started or Done
     * option of a Status.
     */
    async removeOption(
      input: {
        boardId: string;
        fieldId: string;
        optionId: string;
        expectedVersion: number;
        replacementOptionId?: string | null;
      },
      options?: ApiRequestOptions,
    ): Promise<void> {
      const query = new URLSearchParams({
        expectedVersion: String(input.expectedVersion),
      });
      if (input.replacementOptionId) {
        query.set("replacementOptionId", input.replacementOptionId);
      }
      await api.delete<void>(
        `${endpoints.boardFields.option(
          input.boardId,
          input.fieldId,
          input.optionId,
        )}?${query.toString()}`,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
    },

    /**
     * `POST .../options/{optionId}/move`: places the option between the named
     * neighbours (none = the end / the start).
     */
    async moveOption(
      input: {
        boardId: string;
        fieldId: string;
        optionId: string;
        expectedVersion: number;
        previousSiblingId?: string | null;
        nextSiblingId?: string | null;
      },
      options?: ApiRequestOptions,
    ): Promise<void> {
      const body: MoveFieldOptionBody = {
        expectedVersion: input.expectedVersion,
        previousSiblingId: input.previousSiblingId ?? null,
        nextSiblingId: input.nextSiblingId ?? null,
      };
      await api.post<void>(
        `${endpoints.boardFields.option(
          input.boardId,
          input.fieldId,
          input.optionId,
        )}/move`,
        body,
        options ?? { idempotencyKey: crypto.randomUUID() },
      );
    },
  };
}
