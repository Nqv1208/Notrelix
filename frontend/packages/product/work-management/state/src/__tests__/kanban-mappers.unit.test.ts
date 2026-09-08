import { describe, expect, it } from "vitest";
import type { FullBoardDtoApi } from "@notrelix/work-management-core";
import { mapFullBoardDto } from "@notrelix/work-management-core";

const dto: FullBoardDtoApi = {
  id: "board-1",
  title: "Pilot board",
  description: null,
  background: "",
  visibility: "unexpected-value",
  members: [],
  lists: [
    {
      id: "group-2",
      title: "Second",
      color: null,
      position: 2,
      isArchived: false,
      cards: [],
    },
    {
      id: "group-1",
      title: "First",
      color: "#123456",
      position: 1,
      isArchived: false,
      cards: [
        {
          id: "card-1",
          title: "Mapped",
          priority: null,
          status: "unknown-status",
          dueDate: null,
          cover: null,
          memberCount: 0,
          members: [],
          labels: [],
          checklistProgress: 0,
          checklistTotal: 0,
          commentCount: 0,
          attachmentCount: 0,
          position: 1,
          fieldValues: null,
        },
      ],
    },
    {
      id: "archived",
      title: "Archived",
      position: 0,
      isArchived: true,
      cards: [],
    },
  ],
};

describe("Kanban read DTO mapping", () => {
  it("normalizes transport nulls/enums and preserves workspace scope", () => {
    const mapped = mapFullBoardDto(dto, { workspaceId: "workspace-1" });
    expect(mapped.board.workspaceId).toBe("workspace-1");
    expect(mapped.board.description).toBeUndefined();
    expect(mapped.board.visibility).toBe("workspace");
    expect(mapped.board.background.value).toBe("var(--background)");
    expect(mapped.groups.map((group) => group.id)).toEqual([
      "group-1",
      "group-2",
    ]);
    expect(mapped.groups[0]!.cards[0]).toMatchObject({
      id: "card-1",
      boardId: "board-1",
      workspaceId: "workspace-1",
      listId: "group-1",
      status: "status-not-started",
      dueDate: undefined,
    });
  });
});
