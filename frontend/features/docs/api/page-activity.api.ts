import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { PageActivity } from "../types"
import type { HistoryDtoApi } from "../types/dto"

function mapHistory(dto: HistoryDtoApi, pageId: string): PageActivity {
  return {
    id: dto.id,
    pageId,
    actorId: dto.actorId,
    action: "edited",
    targetLabel: dto.resourceTitle ?? dto.action,
    createdAt: dto.createdAt,
  }
}

export const pageActivityApi = {
  async getHistory(pageId: string): Promise<PageActivity[]> {
    const history = await api.get<HistoryDtoApi[]>(endpoints.pages.history(pageId))
    return history.map((item) => mapHistory(item, pageId))
  },
}
