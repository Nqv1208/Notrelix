import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { PageActivity } from "../types/page.types"
import type { HistoryDtoApi } from "../../shared/types/dto"
import { mapHistory } from "../model/page.mapper"

export const pageActivityApi = {
  async getHistory(pageId: string): Promise<PageActivity[]> {
    const history = await api.get<HistoryDtoApi[]>(endpoints.pages.history(pageId))
    return history.map((item) => mapHistory(item, pageId))
  },
}
