import { mockBoardService } from "../mock/mock-service"
import type { UpdateCardInput, UpdateFieldValueInput } from "../schemas"
import type { MoveCardInput } from "../schemas/move-card.schema"

export const boardsApi = {
  // TODO(api):
  // Replace mockBoardService with real HTTP client.
  // Endpoint: GET /api/v1/boards/{boardId}/full
  getFullBoard: (boardId: string) => mockBoardService.getFullBoard(boardId),

  // TODO(api):
  // Endpoint: GET /api/v1/cards/{cardId}
  getCard: (cardId: string) => mockBoardService.getCard(cardId),

  // TODO(api):
  // Endpoint: GET /api/v1/cards/{cardId}/comments
  getCardComments: (cardId: string) => mockBoardService.getCardComments(cardId),

  // TODO(api):
  // Endpoint: GET /api/v1/cards/{cardId}/activity
  getCardActivity: (cardId: string) => mockBoardService.getCardActivity(cardId),

  // TODO(api):
  // Endpoint: PATCH /api/v1/cards/{cardId}
  updateCard: (cardId: string, patch: UpdateCardInput) => mockBoardService.updateCard(cardId, patch),

  // TODO(api):
  // Endpoint: PATCH /api/v1/cards/{cardId}/field-values
  updateFieldValue: (payload: UpdateFieldValueInput) => mockBoardService.updateFieldValue(payload),

  // TODO(api):
  // Endpoint: POST /api/v1/cards/{cardId}/move
  moveCard: (payload: MoveCardInput) => mockBoardService.moveCard(payload),
}
