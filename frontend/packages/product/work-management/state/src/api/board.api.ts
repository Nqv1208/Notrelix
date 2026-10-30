import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type { OperationRequestBody } from "@notrelix/contracts";
import type {
  Board,
  BoardDtoApi,
  BoardViewDtoApi,
  FullBoardDtoApi,
  FullBoardResponse,
  SavedBoardView,
  ViewConfig,
  ViewMode,
} from "@notrelix/work-management-core";
import { mapBoardDto, mapFullBoardDto } from "./board-api-mappers";

/**
 * Table columns a new view does not show ("Cột 9/15"): start date, created
 * and completed. The ids are the table's column ids, not field ids.
 */
export const DEFAULT_HIDDEN_TABLE_COLUMNS = [
  "start",
  "created",
  "completed",
  "checklist",
  "comments",
  "files",
];

export const defaultTableViewConfig: ViewConfig = {
  groupBy: "list",
  hiddenFields: DEFAULT_HIDDEN_TABLE_COLUMNS,
  columnOrder: [],
  columnWidths: {},
  collapsedGroups: {},
  filters: [],
  sortBy: [],
  density: "comfortable",
};

type SaveBoardViewOp = "WorkManagement.BoardViews.Save";
type SaveBoardViewBody = OperationRequestBody<SaveBoardViewOp>;

export function createBoardApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getBoardsByWorkspaceId(workspaceId: string): Promise<Board[]> {
      const boards = await api.get<BoardDtoApi[]>(
        endpoints.boards.listByWorkspaceId(workspaceId),
      );
      return boards.map(mapBoardDto);
    },

    async getFullBoard(
      boardId: string,
      context: { workspaceId: string },
    ): Promise<FullBoardResponse> {
      const board = await api.get<FullBoardDtoApi>(
        endpoints.boards.full(boardId),
      );
      return mapFullBoardDto(board, context);
    },

    async getBoardView(
      boardId: string,
    ): Promise<{ viewMode: ViewMode; viewConfig: ViewConfig }> {
      const view = await api.get<BoardViewDtoApi>(
        endpoints.boardViews.detail(boardId),
      );
      return parseBoardView(view);
    },

    async saveBoardView(
      boardId: string,
      input: { viewMode: ViewMode; viewConfig: ViewConfig },
    ): Promise<void> {
      const config = JSON.stringify(input.viewConfig);
      const body: SaveBoardViewBody = {
        viewMode: input.viewMode,
        config,
        filters: config,
      };
      await api.put<void>(endpoints.boardViews.detail(boardId), body);
    },

    /** The saved views the caller sees: the board's shared ones and their own private ones. */
    async listSavedViews(boardId: string): Promise<SavedBoardView[]> {
      const views = await api.get<BoardViewDtoApi[]>(
        endpoints.boardViews.saved(boardId),
      );
      return views.map(mapSavedView);
    },

    async createSavedView(
      boardId: string,
      input: {
        name: string;
        viewMode: ViewMode;
        viewConfig: ViewConfig;
        visibility: "board" | "me";
      },
    ): Promise<SavedBoardView> {
      const view = await api.post<BoardViewDtoApi>(
        endpoints.boardViews.detail(boardId),
        {
          name: input.name,
          viewMode: input.viewMode,
          config: JSON.stringify(input.viewConfig),
          visibility: input.visibility === "me" ? "Private" : "Board",
        },
      );
      return mapSavedView(view);
    },

    /** Every write on a saved view names the version the caller saw; a stale one changes nothing (409). */
    async updateSavedViewConfig(
      boardId: string,
      viewId: string,
      viewConfig: ViewConfig,
      expectedVersion: number,
    ): Promise<{ version: number }> {
      const view = await api.patch<BoardViewDtoApi>(
        endpoints.boardViews.view(boardId, viewId),
        { configJson: JSON.stringify(viewConfig), expectedVersion },
      );
      return { version: view?.version ?? expectedVersion };
    },

    async renameSavedView(
      boardId: string,
      viewId: string,
      name: string,
      expectedVersion: number,
    ): Promise<void> {
      await api.patch<void>(
        `${endpoints.boardViews.view(boardId, viewId)}/rename`,
        { name, expectedVersion },
      );
    },

    async setSavedViewVisibility(
      boardId: string,
      viewId: string,
      visibility: "board" | "me",
      expectedVersion: number,
    ): Promise<void> {
      await api.patch<void>(
        `${endpoints.boardViews.view(boardId, viewId)}/visibility`,
        {
          visibility: visibility === "me" ? "Private" : "Board",
          expectedVersion,
        },
      );
    },

    async setDefaultSavedView(
      boardId: string,
      viewId: string,
      expectedVersion: number,
    ): Promise<void> {
      await api.post<void>(
        `${endpoints.boardViews.view(boardId, viewId)}/set-default`,
        { expectedVersion },
      );
    },

    /** Deleting the default view hands the default to `newDefaultViewId` (else the oldest shared view). */
    async deleteSavedView(
      boardId: string,
      viewId: string,
      expectedVersion: number,
      newDefaultViewId?: string,
    ): Promise<void> {
      const query = new URLSearchParams({
        expectedVersion: String(expectedVersion),
      });
      if (newDefaultViewId) query.set("newDefaultViewId", newDefaultViewId);
      await api.delete<void>(
        `${endpoints.boardViews.view(boardId, viewId)}?${query.toString()}`,
      );
    },
  };
}

function mapSavedView(view: BoardViewDtoApi): SavedBoardView {
  const { viewMode, viewConfig } = parseBoardView(view);
  return {
    id: view.id ?? "",
    name: view.name ?? "",
    viewMode,
    viewConfig,
    isDefault: view.isDefault === true,
    visibility: view.visibility === "Private" ? "me" : "board",
    createdBy: view.createdBy ?? null,
    version: view.version ?? 0,
  };
}

function parseBoardView(view: BoardViewDtoApi): {
  viewMode: ViewMode;
  viewConfig: ViewConfig;
} {
  // The producer names the layout `type`.
  const viewMode = normalizeViewMode(view?.type ?? view?.viewMode);
  const rawConfig = view?.config ?? view?.filters;
  const parsedConfig =
    typeof rawConfig === "string" ? safeParse(rawConfig) : rawConfig;
  const viewConfig = {
    ...defaultTableViewConfig,
    ...(parsedConfig && typeof parsedConfig === "object" ? parsedConfig : {}),
  };
  return {
    viewMode,
    // An older saved config may carry the retired "default" density.
    viewConfig: {
      ...viewConfig,
      density: viewConfig.density === "compact" ? "compact" : "comfortable",
    },
  };
}

function safeParse(value: string) {
  try {
    return JSON.parse(value);
  } catch {
    return {};
  }
}

function normalizeViewMode(value?: string | null): ViewMode {
  const normalized = value?.trim().toLowerCase();
  if (
    normalized === "kanban" ||
    normalized === "calendar" ||
    normalized === "timeline"
  )
    return normalized;
  return "table";
}
