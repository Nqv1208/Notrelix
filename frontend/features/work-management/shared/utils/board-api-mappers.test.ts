import { describe, expect, test } from "bun:test"
import { mapBoardDto, mapFullBoardDto } from "./board-api-mappers"
import type { BoardDtoApi, FullBoardDtoApi } from "../types/api-types"

describe("board API mappers", () => {
  test("maps workspace board DTO into frontend board model", () => {
    const dto: BoardDtoApi = {
      id: "11111111-1111-1111-1111-111111111111",
      workspaceId: "22222222-2222-2222-2222-222222222222",
      title: "Product delivery",
      description: null,
      background: "ocean",
      visibility: "Workspace",
      isArchived: false,
      memberCount: 3,
      listCount: 4,
      createdAt: "2026-05-22T00:00:00.000Z",
    }

    const board = mapBoardDto(dto)

    expect(board.id).toBe(dto.id)
    expect(board.workspaceId).toBe(dto.workspaceId)
    expect(board.description).toBeUndefined()
    expect(board.background).toEqual({ type: "color", value: "ocean" })
    expect(board.visibility).toBe("workspace")
    expect(board.fieldDefinitions).toEqual([])
    expect(board.members).toEqual([])
  })

  test("maps full board DTO into ordered table groups, backend fields, and card field values", () => {
    const dto: FullBoardDtoApi = {
      id: "33333333-3333-3333-3333-333333333333",
      title: "API-backed board",
      description: "Loaded from backend",
      background: "default",
      visibility: "Private",
      columns: [
        {
          id: "99999999-9999-9999-9999-999999999999",
          boardId: "33333333-3333-3333-3333-333333333333",
          name: "Task",
          fieldType: "text",
          settings: {},
          position: 1,
          isHidden: false,
          isSystemField: true,
        },
        {
          id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          boardId: "33333333-3333-3333-3333-333333333333",
          name: "Priority",
          fieldType: "select",
          settings: {
            options: [
              { id: "urgent", label: "Urgent", color: "var(--destructive)" },
              { id: "high", label: "High", color: "var(--primary)" },
              { id: "medium", label: "Medium", color: "var(--accent)" },
            ],
          },
          position: 2,
          isHidden: false,
          isSystemField: true,
        },
        {
          id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          boardId: "33333333-3333-3333-3333-333333333333",
          name: "Status",
          fieldType: "select",
          settings: {
            options: [
              { id: "status-working", label: "Working on it", color: "var(--primary)" },
              { id: "status-done", label: "Done", color: "var(--accent)" },
            ],
          },
          position: 3,
          isHidden: false,
          isSystemField: true,
        },
      ],
      members: [
        {
          userId: "44444444-4444-4444-4444-444444444444",
          name: "Ada Lovelace",
          avatar: null,
          role: "Owner",
          joinedAt: "2026-05-21T00:00:00.000Z",
        },
      ],
      lists: [
        {
          id: "55555555-5555-5555-5555-555555555555",
          title: "Done",
          position: 2,
          isArchived: false,
          cards: [
            {
              id: "66666666-6666-6666-6666-666666666666",
              title: "Ship docs",
              priority: "High",
              status: "Done",
              dueDate: "2026-05-29T00:00:00.000Z",
              cover: null,
              memberCount: 1,
              checklistProgress: 2,
              checklistTotal: 3,
              commentCount: 4,
              attachmentCount: 5,
              position: 10.5,
            },
          ],
        },
        {
          id: "77777777-7777-7777-7777-777777777777",
          title: "Working on it",
          position: 1,
          isArchived: false,
          cards: [
            {
              id: "88888888-8888-8888-8888-888888888888",
              title: "Wire board API",
              priority: "NotARealPriority",
              status: "Working",
              dueDate: null,
              cover: null,
              memberCount: 0,
              checklistProgress: 0,
              checklistTotal: 0,
              commentCount: 0,
              attachmentCount: 0,
              position: 1.25,
            },
          ],
        },
      ],
    }

    const fullBoard = mapFullBoardDto(dto, { workspaceId: "22222222-2222-2222-2222-222222222222" })
    const titleFieldId = "99999999-9999-9999-9999-999999999999"
    const priorityFieldId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    const statusFieldId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

    expect(fullBoard.board.workspaceId).toBe("22222222-2222-2222-2222-222222222222")
    expect(fullBoard.board.members[0]).toMatchObject({
      userId: "44444444-4444-4444-4444-444444444444",
      name: "Ada Lovelace",
      initials: "AL",
      role: "owner",
    })
    expect(fullBoard.fieldDefinitions.map((field) => field.id)).toEqual([titleFieldId, priorityFieldId, statusFieldId])
    expect(fullBoard.fieldDefinitions[1].options).toContainEqual({
      id: "high",
      label: "High",
      color: "var(--primary)",
    })
    expect(fullBoard.groups.map((group) => group.title)).toEqual(["Working on it", "Done"])
    expect(fullBoard.groups[0].cards[0].position).toBe(1.25)
    expect(fullBoard.groups[0].cards[0].priority).toBe("medium")
    expect(fullBoard.groups[0].cards[0].fieldValues[titleFieldId]).toBe("Wire board API")
    expect(fullBoard.groups[0].cards[0].fieldValues[priorityFieldId]).toBe("medium")
    expect(fullBoard.groups[1].cards[0]._count).toEqual({
      comments: 4,
      attachments: 5,
      checklistItems: 3,
    })
  })
})
