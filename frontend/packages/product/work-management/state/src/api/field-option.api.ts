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
  };
}
