import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type {
  ActivityLogResponseApi,
  AttachmentDtoApi,
  Card,
  CardDetail,
  CardDtoApi,
} from "../types"
import type { CreateCardInput, UpdateCardInput, UpdateFieldValueInput, UploadCardFileInput } from "../schemas"
import type { MoveCardInput } from "../schemas/move-card.schema"
import { mapActivityResponse, mapAttachmentDtoToCardFile, mapCardDto } from "../utils/board-api-mappers"

export const cardApi = {
  async getCard(cardId: string): Promise<CardDetail> {
    const card = await api.get<CardDtoApi>(endpoints.cards.detail(cardId))
    return mapCardDto(card)
  },

  async createCard(_boardId: string, payload: CreateCardInput): Promise<Card> {
    const id = await api.post<string>(endpoints.lists.cards(payload.listId), {
      title: payload.title,
      position: payload.position,
    })
    return this.getCard(id)
  },

  async updateCard(cardId: string, patch: UpdateCardInput): Promise<void> {
    await api.patch<void>(endpoints.cards.detail(cardId), patch)
  },

  async deleteCard(cardId: string): Promise<void> {
    await api.delete<void>(endpoints.cards.detail(cardId))
  },

  async archiveCard(cardId: string): Promise<void> {
    await api.post<void>(endpoints.cards.archive(cardId))
  },

  async duplicateCard(cardId: string): Promise<string> {
    return api.post<string>(endpoints.cards.duplicate(cardId))
  },

  async moveCard(payload: MoveCardInput): Promise<void> {
    await api.post<void>(endpoints.cards.move(payload.cardId), {
      listId: payload.listId,
      position: payload.position,
    })
  },

  async updateFieldValue(payload: UpdateFieldValueInput): Promise<void> {
    await api.patch<void>(endpoints.cards.fieldValues(payload.cardId), {
      fieldDefinitionId: payload.fieldDefinitionId,
      value: payload.value,
    })
  },

  async getCardFiles(cardId: string) {
    const files = await api.get<AttachmentDtoApi[]>(endpoints.cards.attachments(cardId))
    return files.map((file) => mapAttachmentDtoToCardFile(file, cardId))
  },

  async getCardActivity(cardId: string) {
    const activity = await api.get<ActivityLogResponseApi>(endpoints.cards.activity(cardId))
    return mapActivityResponse(activity, cardId)
  },

  async uploadCardFile(input: UploadCardFileInput) {
    void input
    throw new Error("Card file upload requires a storage presign URL before attachment metadata can be registered.")
  },
}
