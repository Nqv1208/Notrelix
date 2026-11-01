import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";

type ListTemplatesResponse = OperationResponse<
  "WorkManagement.Templates.List",
  200
>;
type SaveBoardBody = OperationRequestBody<"WorkManagement.Templates.Create">;
type CreateBoardBody =
  OperationRequestBody<"WorkManagement.Templates.CreateBoard">;

export type BoardTemplateStatus = "draft" | "published" | "archived";

/** A board template of the workspace, or one of the built-in starters (`builtInKey` set). */
export interface BoardTemplate {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly status: BoardTemplateStatus;
  readonly builtInKey: string | null;
}

export interface SaveBoardAsTemplateInput {
  readonly boardId: string;
  readonly name: string;
  readonly description?: string;
  readonly includeItems?: boolean;
}

export interface CreateBoardFromTemplateInput {
  readonly templateId: string;
  readonly workspaceId: string;
  readonly name: string;
  readonly spaceId?: string | null;
}

const STATUSES: readonly BoardTemplateStatus[] = [
  "draft",
  "published",
  "archived",
];

function toStatus(raw: string | null | undefined): BoardTemplateStatus {
  const status = (raw ?? "").toLowerCase();
  return STATUSES.find((entry) => entry === status) ?? "draft";
}

/** The workspace's board templates: list, save a board as one, manage and create a board from one. */
export function createTemplateApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async list(workspaceId: string): Promise<BoardTemplate[]> {
      const rows = await api.get<ListTemplatesResponse>(
        endpoints.boardTemplates.list(workspaceId),
      );
      return (rows ?? []).flatMap((row) =>
        row.id
          ? [
              {
                id: row.id,
                name: row.name ?? "",
                description: row.description ?? null,
                status: toStatus(row.status),
                builtInKey: row.builtInKey ?? null,
              },
            ]
          : [],
      );
    },

    /** Resolves with the new template's id. */
    async saveBoard(input: SaveBoardAsTemplateInput): Promise<string> {
      const body: SaveBoardBody = {
        name: input.name,
        description: input.description,
        includeItems: input.includeItems ?? false,
      };
      return api.post<string>(
        endpoints.boardTemplates.saveBoard(input.boardId),
        body,
      );
    },

    async publish(templateId: string): Promise<void> {
      await api.patch<void>(endpoints.boardTemplates.publish(templateId));
    },

    async archive(templateId: string): Promise<void> {
      await api.patch<void>(endpoints.boardTemplates.archive(templateId));
    },

    async remove(templateId: string): Promise<void> {
      await api.delete<void>(endpoints.boardTemplates.detail(templateId));
    },

    /** Resolves with the new board's id. */
    async createBoard(input: CreateBoardFromTemplateInput): Promise<string> {
      const body: CreateBoardBody = {
        workspaceId: input.workspaceId,
        name: input.name,
        ...(input.spaceId ? { spaceId: input.spaceId } : {}),
      };
      return api.post<string>(
        endpoints.boardTemplates.createBoard(input.templateId),
        body,
      );
    },
  };
}
