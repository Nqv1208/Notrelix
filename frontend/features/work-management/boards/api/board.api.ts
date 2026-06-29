import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { Board, BoardDtoApi, BoardViewDtoApi, FullBoardDtoApi, FullBoardResponse, ViewConfig, ViewMode } from "@/features/work-management/types"
import { mapBoardDto, mapFullBoardDto } from "@/features/work-management/shared/utils/board-api-mappers"

export const defaultTableViewConfig: ViewConfig = {
  groupBy: "list",
  hiddenFields: [],
  columnOrder: [],
  columnWidths: {},
  collapsedGroups: {},
  filters: [],
  sortBy: [],
}

export const boardApi = {
  async getBoardsByWorkspaceId(workspaceId: string): Promise<Board[]> {
    const boards = await api.get<BoardDtoApi[]>(endpoints.boards.listByWorkspaceId(workspaceId))
    return boards.map(mapBoardDto)
  },

  async getFullBoard(boardId: string, context: { workspaceId: string }): Promise<FullBoardResponse> {
    const board = await api.get<FullBoardDtoApi>(endpoints.boards.full(boardId))
    return mapFullBoardDto(board, context)
  },

  async getBoardView(boardId: string): Promise<{ viewMode: ViewMode; viewConfig: ViewConfig }> {
    const view = await api.get<BoardViewDtoApi>(endpoints.boards.view(boardId))
    return parseBoardView(view)
  },

  async saveBoardView(boardId: string, input: { viewMode: ViewMode; viewConfig: ViewConfig }): Promise<void> {
    const config = JSON.stringify(input.viewConfig)
    await api.put<void>(endpoints.boards.view(boardId), {
      viewMode: input.viewMode,
      config,
      filters: config,
    })
  },
}

function parseBoardView(view: BoardViewDtoApi): { viewMode: ViewMode; viewConfig: ViewConfig } {
  const viewMode = normalizeViewMode(view.viewMode)
  const rawConfig = view.config ?? view.filters
  const parsedConfig = typeof rawConfig === "string" ? safeParse(rawConfig) : rawConfig
  return {
    viewMode,
    viewConfig: {
      ...defaultTableViewConfig,
      ...(parsedConfig && typeof parsedConfig === "object" ? parsedConfig : {}),
    },
  }
}

function safeParse(value: string) {
  try {
    return JSON.parse(value)
  } catch {
    return {}
  }
}

function normalizeViewMode(value?: string | null): ViewMode {
  const normalized = value?.trim().toLowerCase()
  if (normalized === "kanban" || normalized === "calendar" || normalized === "timeline") return normalized
  return "table"
}
