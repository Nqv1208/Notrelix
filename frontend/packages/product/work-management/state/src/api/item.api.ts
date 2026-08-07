import type { ApiRequestOptions, NotrelixClient } from "@notrelix/contracts"
import { endpoints } from "@notrelix/contracts"
import type {
  ActivityLogResponseApi,
  AttachmentDtoApi,
  Card,
  CardDetail,
  CardDtoApi,
} from "@notrelix/work-management-core"
import type { CreateCardInput, UpdateCardInput, UpdateFieldValueInput, UploadCardFileInput } from "@notrelix/work-management-core"
import type { MoveCardInput } from "@notrelix/work-management-core"
import { mapActivityResponse, mapAttachmentDtoToCardFile, mapCardDto } from "@notrelix/work-management-core"

export function createCardApi(client: NotrelixClient) {
  const api = client.api;
  return {
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

    async moveCard(payload: MoveCardInput, options?: ApiRequestOptions): Promise<void> {
      const body = {
        listId: payload.listId,
        position: payload.position,
      }
      if (options) {
        await api.post<void>(endpoints.cards.move(payload.cardId), body, options)
        return
      }
      await api.post<void>(endpoints.cards.move(payload.cardId), body)
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
  };
}
