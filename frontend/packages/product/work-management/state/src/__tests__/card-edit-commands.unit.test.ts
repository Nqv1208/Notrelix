import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type {
  CardDetail,
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { runFieldValueCommand } from "../mutations/use-update-field-value";
import { runUpdateCardCommand } from "../mutations/use-update-card";

const PRIORITY = "field-priority-x";
const priorityField: FieldDefinition = {
  id: PRIORITY,
  boardId: "b-1",
  name: "Ưu tiên",
  fieldType: "select",
  semanticRole: "priority",
  position: 2,
  isHidden: false,
  isSystemField: false,
  options: [
    { id: "p-high", label: "Cao", color: "" },
    { id: "p-low", label: "Thấp", color: "" },
  ],
};

const baseCard = {
  id: "i-1",
  listId: "g-1",
  boardId: "b-1",
  workspaceId: "ws-1",
  title: "Việc cũ",
  version: 3,
  position: 1,
  status: "",
  priority: "low" as const,
  isArchived: false,
  isDeleted: false,
  members: [],
  labels: [],
  checklists: [],
  fieldValues: { [PRIORITY]: "p-low" },
  _count: { comments: 0, attachments: 0, checklistItems: 0 },
  createdAt: "2026-10-01T00:00:00.000Z",
};

function seeded() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  const boardKey = wmQueryKeys.fullBoard("ws-1", "b-1");
  const detailKey = wmQueryKeys.cardDetail("ws-1", "i-1");
  const board: FullBoardResponse = {
    board: {
      id: "b-1",
      workspaceId: "ws-1",
      title: "Board",
      background: { type: "color", value: "#fff" },
      visibility: "workspace",
      isArchived: false,
      fieldDefinitions: [priorityField],
      members: [],
      createdAt: "2026-10-01T00:00:00.000Z",
    },
    groups: [
      {
        id: "g-1",
        title: "Kế hoạch",
        position: 1,
        isCollapsed: false,
        cards: [baseCard],
      },
    ],
    fieldDefinitions: [priorityField],
  };
  const detail: CardDetail = {
    ...baseCard,
    boardTitle: "Board",
    watchers: [],
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  };
  queryClient.setQueryData(boardKey, board);
  queryClient.setQueryData(detailKey, detail);
  return { queryClient, boardKey, detailKey };
}

const ASSIGNEE = "field-assignee-x";
const assigneeField: FieldDefinition = {
  id: ASSIGNEE,
  boardId: "b-1",
  name: "Assignee",
  fieldType: "person",
  position: 1,
  isHidden: false,
  isSystemField: true,
  options: [],
};

describe("the Assignee system field is written through the item's assignees", () => {
  it("adds the chosen person and removes the one no longer chosen, never writing a field value", async () => {
    const { queryClient, boardKey } = seeded();
    const board = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
    board.board.fieldDefinitions = [priorityField, assigneeField];
    board.groups[0]!.cards[0]!.members = [
      { id: "m-1", userId: "u-old", name: "Old", initials: "O" },
    ] as never;
    queryClient.setQueryData(boardKey, board);
    const updateFieldValue = vi.fn(async () => undefined);
    const setAssignees = vi.fn(async () => undefined);
    await runFieldValueCommand(
      queryClient,
      { updateFieldValue, setAssignees },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        payload: {
          cardId: "i-1",
          fieldDefinitionId: ASSIGNEE,
          value: ["u-new"],
        },
      },
    );
    expect(setAssignees).toHaveBeenCalledWith("i-1", ["u-new"], ["u-old"], 3);
    expect(updateFieldValue).not.toHaveBeenCalled();
  });
});

describe("sheet edits reach the item detail the sheet reads", () => {
  it("a field value shows at once in the board and in the item detail, and both are read again", async () => {
    const { queryClient, boardKey, detailKey } = seeded();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    let release!: () => void;
    const updateFieldValue = vi.fn(
      () => new Promise<void>((resolve) => (release = resolve)),
    );
    const pending = runFieldValueCommand(
      queryClient,
      { updateFieldValue },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        payload: {
          cardId: "i-1",
          fieldDefinitionId: PRIORITY,
          value: "p-high",
        },
      },
    );
    await vi.waitFor(() => expect(updateFieldValue).toHaveBeenCalled());
    const detail = queryClient.getQueryData<CardDetail>(detailKey)!;
    expect(detail.fieldValues[PRIORITY]).toBe("p-high");
    expect(detail.priority).toBe("high");
    const board = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
    expect(board.groups[0]!.cards[0]!.priority).toBe("high");
    release();
    await pending;
    const invalidated = invalidate.mock.calls.map(
      (call) => (call[0] as { queryKey: readonly unknown[] }).queryKey,
    );
    expect(invalidated).toContainEqual(detailKey);
    expect(invalidated).toContainEqual(boardKey);
  });

  it("a failed field value puts the item detail back", async () => {
    const { queryClient, detailKey } = seeded();
    await expect(
      runFieldValueCommand(
        queryClient,
        {
          updateFieldValue: async () => {
            throw new Error("forbidden");
          },
        },
        {
          workspaceId: "ws-1",
          boardId: "b-1",
          payload: {
            cardId: "i-1",
            fieldDefinitionId: PRIORITY,
            value: "p-high",
          },
        },
      ),
    ).rejects.toThrow("forbidden");
    expect(
      queryClient.getQueryData<CardDetail>(detailKey)!.fieldValues[PRIORITY],
    ).toBe("p-low");
  });

  it("a title edit shows in the item detail at once", async () => {
    const { queryClient, detailKey } = seeded();
    let release!: () => void;
    const updateCard = vi.fn(
      () => new Promise<void>((resolve) => (release = resolve)),
    );
    const pending = runUpdateCardCommand(
      queryClient,
      { updateCard },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        cardId: "i-1",
        patch: { title: "Việc mới" },
      },
    );
    await vi.waitFor(() => expect(updateCard).toHaveBeenCalled());
    expect(queryClient.getQueryData<CardDetail>(detailKey)!.title).toBe(
      "Việc mới",
    );
    expect(updateCard).toHaveBeenCalledWith(
      "i-1",
      { title: "Việc mới" },
      expect.objectContaining({ expectedVersion: 3 }),
    );
    release();
    await pending;
  });
});
