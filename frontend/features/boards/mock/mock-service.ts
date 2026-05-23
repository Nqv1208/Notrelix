import { mockDelay } from "./mock-delay"
import { mockBoards, mockCardActivity, mockCardComments } from "./mock-data"
import type { Card, CardActivity, CardComment, FullBoardResponse } from "../types"
import type { UpdateCardInput, UpdateFieldValueInput } from "../schemas"
import type { MoveCardInput } from "../schemas/move-card.schema"

function cloneBoard(board: FullBoardResponse): FullBoardResponse {
  return {
    board: { ...board.board, fieldDefinitions: [...board.board.fieldDefinitions], members: [...board.board.members] },
    fieldDefinitions: [...board.fieldDefinitions],
    groups: board.groups.map((group) => ({
      ...group,
      cards: group.cards.map((card) => ({
        ...card,
        members: [...card.members],
        labels: [...card.labels],
        checklists: card.checklists.map((checklist) => ({
          ...checklist,
          items: [...checklist.items],
        })),
        fieldValues: { ...card.fieldValues },
        _count: { ...card._count },
      })),
    })),
  }
}

export const mockBoardService = {
  // TODO(api):
  // Replace with real API integration.
  // Endpoint: GET /api/v1/boards/{boardId}/full
  // Hook: useFullBoard
  async getFullBoard(boardId: string): Promise<FullBoardResponse> {
    await mockDelay()
    const board = mockBoards.find((item) => item.board.id === boardId) ?? mockBoards[0]
    return cloneBoard(board)
  },

  // TODO(api):
  // Replace with real API integration.
  // Endpoint: GET /api/v1/cards/{cardId}
  // Hook: useCard
  async getCard(cardId: string): Promise<Card> {
    await mockDelay()
    const card = findCard(cardId)
    return {
      ...card,
      members: [...card.members],
      labels: [...card.labels],
      checklists: card.checklists.map((checklist) => ({ ...checklist, items: [...checklist.items] })),
      fieldValues: { ...card.fieldValues },
      _count: { ...card._count },
    }
  },

  // TODO(api):
  // Replace with real API integration.
  // Endpoint: GET /api/v1/cards/{cardId}/comments
  // Hook: useCardComments
  async getCardComments(cardId: string): Promise<CardComment[]> {
    await mockDelay(150, 320)
    const comments = mockCardComments.filter((comment) => comment.cardId === cardId)
    if (comments.length > 0) return comments
    return [
      {
        id: `${cardId}-comment-fallback`,
        cardId,
        author: "Ana Moreno",
        body: "Add context here so the team can understand the latest decision.",
        createdAt: "2026-05-13T11:00:00.000Z",
      },
    ]
  },

  // TODO(api):
  // Replace with real API integration.
  // Endpoint: GET /api/v1/cards/{cardId}/activity
  // Hook: useCardActivity
  async getCardActivity(cardId: string): Promise<CardActivity[]> {
    await mockDelay(150, 320)
    const activity = mockCardActivity.filter((item) => item.cardId === cardId)
    if (activity.length > 0) return activity
    return [
      {
        id: `${cardId}-activity-fallback`,
        cardId,
        actor: "Minh Tran",
        action: "updated card details",
        createdAt: "2026-05-13T11:08:00.000Z",
      },
    ]
  },

  // TODO(api):
  // Replace with real API integration.
  // Endpoint: PATCH /api/v1/cards/{cardId}
  async updateCard(cardId: string, patch: UpdateCardInput): Promise<Card> {
    await mockDelay(120, 300)
    const card = findCard(cardId)
    if (patch.title !== undefined) card.title = patch.title
    if (patch.descriptionMd !== undefined) card.descriptionMd = patch.descriptionMd
    if (patch.priority !== undefined) card.priority = patch.priority ?? undefined
    if (patch.dueDate !== undefined) card.dueDate = patch.dueDate ?? undefined
    if (patch.startDate !== undefined) card.startDate = patch.startDate ?? undefined
    card.updatedAt = new Date().toISOString()
    if (patch.title !== undefined) {
      const titleFieldId = `${card.boardId}-field-title`
      card.fieldValues[titleFieldId] = patch.title
    }
    if (patch.dueDate !== undefined) {
      const dueFieldId = `${card.boardId}-field-due-date`
      card.fieldValues[dueFieldId] = patch.dueDate
    }
    if (patch.priority !== undefined) {
      const priorityFieldId = `${card.boardId}-field-priority`
      card.fieldValues[priorityFieldId] = patch.priority
    }
    return { ...card, fieldValues: { ...card.fieldValues } }
  },

  // TODO(api):
  // Replace with real API integration.
  // Endpoint: PATCH /api/v1/cards/{cardId}/field-values
  async updateFieldValue(payload: UpdateFieldValueInput): Promise<Card> {
    await mockDelay(120, 300)
    const card = findCard(payload.cardId)
    card.fieldValues[payload.fieldDefinitionId] = payload.value
    if (payload.fieldDefinitionId.endsWith("field-status") && typeof payload.value === "string") {
      card.status = payload.value
    }
    if (payload.fieldDefinitionId.endsWith("field-priority")) {
      card.priority = payload.value as Card["priority"]
    }
    card.updatedAt = new Date().toISOString()
    return { ...card, fieldValues: { ...card.fieldValues } }
  },

  // TODO(api):
  // Replace with real API integration.
  // Endpoint: POST /api/v1/cards/{cardId}/move
  async moveCard(payload: MoveCardInput): Promise<Card> {
    await mockDelay(120, 320)
    const board = mockBoards.find((item) => item.groups.some((group) => group.cards.some((card) => card.id === payload.cardId)))
    if (!board) throw new Error("Board not found")
    const sourceGroup = board.groups.find((group) => group.cards.some((card) => card.id === payload.cardId))
    const targetGroup = board.groups.find((group) => group.id === payload.listId)
    if (!sourceGroup || !targetGroup) throw new Error("List not found")
    const cardIndex = sourceGroup.cards.findIndex((card) => card.id === payload.cardId)
    const [card] = sourceGroup.cards.splice(cardIndex, 1)
    card.listId = payload.listId
    card.position = payload.position
    card.updatedAt = new Date().toISOString()
    targetGroup.cards.push(card)
    targetGroup.cards.sort((a, b) => a.position - b.position)
    return { ...card, fieldValues: { ...card.fieldValues } }
  },
}

function findCard(cardId: string) {
  for (const board of mockBoards) {
    for (const group of board.groups) {
      const card = group.cards.find((item) => item.id === cardId)
      if (card) return card
    }
  }
  throw new Error("Card not found")
}
