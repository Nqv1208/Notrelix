import { mockDelay } from "./mock-delay"
import { mockBoards, mockCardActivity, mockCardComments } from "./mock-data"
import type { CreateCardUpdateInput, UploadCardFileInput } from "../schemas"
import type { BoardMember, Card, CardActivity, CardDetail, CardFile, CardMember, CardUpdate } from "../types"

const cardUpdates: CardUpdate[] = mockCardComments.map((comment, index) => {
  const { board } = findBoardAndCard(comment.cardId)
  const author = memberToCardMember(board.board.members[index % board.board.members.length], comment.cardId)

  return {
    id: comment.id.replace("comment", "update"),
    cardId: comment.cardId,
    author,
    body: comment.body,
    mentionUserIds: [],
    attachmentIds: [],
    createdAt: comment.createdAt,
  }
})

const cardFiles: CardFile[] = mockBoards.flatMap((board) =>
  board.groups.flatMap((group) =>
    group.cards.flatMap((card) =>
      Array.from({ length: card._count.attachments }).map((_, index) => ({
        id: `${card.id}-file-${index + 1}`,
        cardId: card.id,
        name: index === 0 ? "product-spec.pdf" : `workspace-asset-${index + 1}.png`,
        size: 128_000 + index * 56_000,
        contentType: index === 0 ? "application/pdf" : "image/png",
        url: `https://r2.notrelix.example/cards/${card.id}/file-${index + 1}`,
        source: "r2" as const,
        createdBy: memberToCardMember(board.board.members[index % board.board.members.length], card.id),
        createdAt: card.updatedAt ?? card.createdAt,
      }))
    )
  )
)

export const mockCardDetailService = {
  // TODO(api):
  // Replace mock service with real API integration.
  // Endpoint: GET /api/v1/cards/:cardId
  async getCardDetail(cardId: string): Promise<CardDetail> {
    await mockDelay()
    const { board, card } = findBoardAndCard(cardId)
    return {
      ...cloneCard(card),
      boardTitle: board.board.title,
      watchers: board.board.members.slice(0, 3).map((member) => memberToCardMember(member, cardId)),
      isWatched: true,
      updates: await mockCardDetailService.getCardUpdates(cardId),
      files: await mockCardDetailService.getCardFiles(cardId),
      activity: await mockCardDetailService.getCardActivity(cardId),
    }
  },

  // TODO(api):
  // Replace mock service with real API integration.
  // Endpoint: GET /api/v1/cards/:cardId
  async getCardUpdates(cardId: string): Promise<CardUpdate[]> {
    await mockDelay(120, 260)
    return cardUpdates
      .filter((update) => update.cardId === cardId)
      .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
      .map((update) => ({ ...update, author: { ...update.author }, mentionUserIds: [...update.mentionUserIds], attachmentIds: [...update.attachmentIds] }))
  },

  // TODO(api):
  // Replace mock service with real API integration.
  // Endpoint: GET /api/v1/cards/:cardId
  async getCardFiles(cardId: string): Promise<CardFile[]> {
    await mockDelay(120, 260)
    return cardFiles
      .filter((file) => file.cardId === cardId)
      .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
      .map((file) => ({ ...file, createdBy: { ...file.createdBy } }))
  },

  // TODO(api):
  // Replace mock service with real API integration.
  // Endpoint: GET /api/v1/cards/:cardId
  async getCardActivity(cardId: string): Promise<CardActivity[]> {
    await mockDelay(120, 260)
    return mockCardActivity
      .filter((activity) => activity.cardId === cardId)
      .map((activity) => ({
        ...activity,
        type: "updated" as const,
        metadata: {},
      }))
  },

  // TODO(api):
  // Replace mock service with real API integration.
  // Endpoint: GET /api/v1/cards/:cardId
  async createCardUpdate(input: CreateCardUpdateInput): Promise<CardUpdate> {
    await mockDelay(140, 300)
    const { board } = findBoardAndCard(input.cardId)
    const author = memberToCardMember(board.board.members[0], input.cardId)
    const update: CardUpdate = {
      id: `update-${createId()}`,
      cardId: input.cardId,
      author,
      body: input.body,
      mentionUserIds: [...input.mentionUserIds],
      attachmentIds: [...input.attachmentIds],
      createdAt: new Date().toISOString(),
    }
    cardUpdates.unshift(update)
    return { ...update, author: { ...update.author }, mentionUserIds: [...update.mentionUserIds], attachmentIds: [...update.attachmentIds] }
  },

  // TODO(api):
  // Replace mock service with real API integration.
  // Endpoint: GET /api/v1/cards/:cardId
  async uploadCardFile(input: UploadCardFileInput): Promise<CardFile> {
    await mockDelay(200, 420)
    const { board } = findBoardAndCard(input.cardId)
    const id = `file-${createId()}`
    const file: CardFile = {
      id,
      cardId: input.cardId,
      name: input.name,
      size: input.size,
      contentType: input.contentType,
      url: `https://r2.notrelix.example/cards/${input.cardId}/${encodeURIComponent(id)}-${encodeURIComponent(input.name)}`,
      source: "r2",
      createdBy: memberToCardMember(board.board.members[0], input.cardId),
      createdAt: new Date().toISOString(),
    }
    cardFiles.unshift(file)
    return { ...file, createdBy: { ...file.createdBy } }
  },
}

function cloneCard(card: Card): Card {
  return {
    ...card,
    members: card.members.map((member) => ({ ...member })),
    labels: card.labels.map((label) => ({ ...label })),
    checklists: card.checklists.map((checklist) => ({
      ...checklist,
      items: checklist.items.map((item) => ({ ...item })),
    })),
    fieldValues: { ...card.fieldValues },
    _count: { ...card._count },
  }
}

function findBoardAndCard(cardId: string) {
  for (const board of mockBoards) {
    for (const group of board.groups) {
      const card = group.cards.find((item) => item.id === cardId)
      if (card) return { board, card }
    }
  }
  throw new Error("Card not found")
}

function memberToCardMember(member: BoardMember, cardId: string): CardMember {
  return {
    id: `cm-${cardId}-${member.userId}`,
    userId: member.userId,
    name: member.name,
    initials: member.initials,
    avatarUrl: member.avatarUrl,
    color: member.color,
  }
}

function createId() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`
}
