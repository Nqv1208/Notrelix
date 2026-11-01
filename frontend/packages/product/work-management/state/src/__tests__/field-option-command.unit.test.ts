import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type {
  Card,
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import {
  FieldOptionWriteError,
  runAddFieldOption,
  runMoveFieldOption,
  runRemoveFieldOption,
  runUpdateFieldOption,
  type FieldOptionWriter,
} from "../mutations/use-field-option";

const FIELD = "field-status";

function statusField(version: number | null = 3): FieldDefinition {
  return {
    id: FIELD,
    boardId: "b-1",
    name: "Trạng thái",
    fieldType: "select",
    semanticRole: "status",
    position: 1,
    isHidden: false,
    isSystemField: true,
    ...(version === null ? {} : { version }),
    options: [
      {
        id: "o-todo",
        label: "Cần làm",
        color: "#7A8391",
        position: "a0",
        category: "todo",
      },
      {
        id: "o-doing",
        label: "Đang làm",
        color: "#1E7BE0",
        position: "a1",
        category: "in_progress",
      },
    ],
  };
}

function fullBoard(field: FieldDefinition): FullBoardResponse {
  return {
    board: {
      id: "b-1",
      workspaceId: "ws-1",
      title: "Board",
      background: { type: "color", value: "#fff" },
      visibility: "workspace",
      isArchived: false,
      fieldDefinitions: [field],
      members: [],
      createdAt: "2026-10-01T00:00:00.000Z",
    },
    groups: [],
    fieldDefinitions: [field],
  };
}

function seeded(field = statusField()) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  const key = wmQueryKeys.fullBoard("ws-1", "b-1");
  queryClient.setQueryData(key, fullBoard(field));
  return { queryClient, key };
}

const target = { workspaceId: "ws-1", boardId: "b-1", fieldId: FIELD };
const draft = {
  name: "Chờ khách duyệt",
  color: "#C18A12",
  category: "in_progress" as const,
};

function options(queryClient: QueryClient, key: readonly unknown[]) {
  return queryClient
    .getQueryData<FullBoardResponse>(key)!
    .board.fieldDefinitions[0]!.options.map((option) => option.label);
}

describe("field option commands", () => {
  it("adds the option last at once and sends the field's version with an idempotency key", async () => {
    const { queryClient, key } = seeded();
    let during: string[] = [];
    const writer: FieldOptionWriter = {
      removeOption: vi.fn(),
      moveOption: vi.fn(),
      addOption: vi.fn(async () => {
        during = options(queryClient, key);
        return { optionId: "o-new", version: 2 };
      }),
      updateOption: vi.fn(),
    };
    await runAddFieldOption(queryClient, writer, target, draft);
    expect(during).toEqual(["Cần làm", "Đang làm", "Chờ khách duyệt"]);
    expect(writer.addOption).toHaveBeenCalledWith(
      {
        boardId: "b-1",
        fieldId: FIELD,
        name: "Chờ khách duyệt",
        color: "#C18A12",
        category: "in_progress",
        expectedVersion: 3,
      },
      expect.objectContaining({
        idempotencyKey: expect.stringMatching(/^add-field-option:/),
      }),
    );
  });

  it("puts the field back and rethrows when the write fails", async () => {
    const { queryClient, key } = seeded();
    const writer: FieldOptionWriter = {
      removeOption: vi.fn(),
      moveOption: vi.fn(),
      addOption: vi.fn(async () => {
        throw new Error("412");
      }),
      updateOption: vi.fn(),
    };
    await expect(
      runAddFieldOption(queryClient, writer, target, draft),
    ).rejects.toThrow("412");
    expect(options(queryClient, key)).toEqual(["Cần làm", "Đang làm"]);
  });

  it("invalidates the board read after success and after failure", async () => {
    const { queryClient, key } = seeded();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    const failing: FieldOptionWriter = {
      removeOption: vi.fn(),
      moveOption: vi.fn(),
      addOption: vi.fn(async () => {
        throw new Error("x");
      }),
      updateOption: vi.fn(async () => undefined),
    };
    await runUpdateFieldOption(
      queryClient,
      failing,
      { ...target, optionId: "o-doing" },
      draft,
    );
    await runAddFieldOption(queryClient, failing, target, draft).catch(
      () => undefined,
    );
    expect(invalidate).toHaveBeenCalledTimes(2);
    expect(invalidate).toHaveBeenCalledWith({ queryKey: key });
  });

  it("renames and recolours one option at once and restores it on failure", async () => {
    const { queryClient, key } = seeded();
    let during: string[] = [];
    const writer: FieldOptionWriter = {
      removeOption: vi.fn(),
      moveOption: vi.fn(),
      addOption: vi.fn(),
      updateOption: vi.fn(async () => {
        during = options(queryClient, key);
        throw new Error("409");
      }),
    };
    await expect(
      runUpdateFieldOption(
        queryClient,
        writer,
        { ...target, optionId: "o-doing" },
        { name: "Đang triển khai", color: "#2E8B57", category: "in_progress" },
      ),
    ).rejects.toThrow("409");
    expect(during).toEqual(["Cần làm", "Đang triển khai"]);
    expect(options(queryClient, key)).toEqual(["Cần làm", "Đang làm"]);
  });

  it("fails closed without a field version or an unknown field and sends nothing", async () => {
    const writer: FieldOptionWriter = {
      removeOption: vi.fn(),
      moveOption: vi.fn(),
      addOption: vi.fn(),
      updateOption: vi.fn(),
    };
    const noVersion = seeded(statusField(null));
    await expect(
      runAddFieldOption(noVersion.queryClient, writer, target, draft),
    ).rejects.toMatchObject({ reason: "version-unknown" });
    const unknown = seeded();
    await expect(
      runAddFieldOption(
        unknown.queryClient,
        writer,
        { ...target, fieldId: "missing" },
        draft,
      ),
    ).rejects.toBeInstanceOf(FieldOptionWriteError);
    expect(writer.addOption).not.toHaveBeenCalled();
    expect(options(noVersion.queryClient, noVersion.key)).toEqual([
      "Cần làm",
      "Đang làm",
    ]);
  });

  it("removes a status at once, re-homes its cards and sends the replacement", async () => {
    const { queryClient, key } = seeded();
    queryClient.setQueryData<FullBoardResponse>(key, (board) => ({
      ...board!,
      groups: [
        {
          id: "g-1",
          title: "G",
          color: "#000",
          position: 1,
          isCollapsed: false,
          cards: [card("c-1", "o-doing"), card("c-2", "o-todo")],
        },
      ],
    }));
    let during: (string | null | undefined)[] = [];
    const writer: FieldOptionWriter = {
      addOption: vi.fn(),
      updateOption: vi.fn(),
      moveOption: vi.fn(),
      removeOption: vi.fn(async () => {
        during = queryClient
          .getQueryData<FullBoardResponse>(key)!
          .groups[0]!.cards.map((entry) => entry.statusOptionId);
      }),
    };
    await runRemoveFieldOption(
      queryClient,
      writer,
      { ...target, optionId: "o-doing" },
      "o-todo",
    );
    expect(during).toEqual(["o-todo", "o-todo"]);
    expect(writer.removeOption).toHaveBeenCalledWith(
      {
        boardId: "b-1",
        fieldId: FIELD,
        optionId: "o-doing",
        expectedVersion: 3,
        replacementOptionId: "o-todo",
      },
      expect.objectContaining({
        idempotencyKey: expect.stringMatching(/^remove-field-option:/),
      }),
    );
  });

  it("puts the option and its cards back when the removal is refused", async () => {
    const { queryClient, key } = seeded();
    queryClient.setQueryData<FullBoardResponse>(key, (board) => ({
      ...board!,
      groups: [
        {
          id: "g-1",
          title: "G",
          color: "#000",
          position: 1,
          isCollapsed: false,
          cards: [card("c-1", "o-doing")],
        },
      ],
    }));
    const writer: FieldOptionWriter = {
      addOption: vi.fn(),
      updateOption: vi.fn(),
      moveOption: vi.fn(),
      removeOption: vi.fn(async () => {
        throw new Error("400");
      }),
    };
    await expect(
      runRemoveFieldOption(
        queryClient,
        writer,
        { ...target, optionId: "o-doing" },
        "o-todo",
      ),
    ).rejects.toThrow("400");
    const board = queryClient.getQueryData<FullBoardResponse>(key)!;
    expect(options(queryClient, key)).toEqual(["Cần làm", "Đang làm"]);
    expect(board.groups[0]!.cards[0]!.statusOptionId).toBe("o-doing");
  });

  it("moves an option after its neighbour: swaps at once and names the neighbours", async () => {
    const { queryClient, key } = seeded();
    let during: string[] = [];
    const writer: FieldOptionWriter = {
      addOption: vi.fn(),
      updateOption: vi.fn(),
      removeOption: vi.fn(),
      moveOption: vi.fn(async () => {
        during = options(queryClient, key);
      }),
    };
    await runMoveFieldOption(
      queryClient,
      writer,
      { ...target, optionId: "o-todo" },
      "after",
    );
    expect(writer.moveOption).toHaveBeenCalledWith(
      {
        boardId: "b-1",
        fieldId: FIELD,
        optionId: "o-todo",
        expectedVersion: 3,
        previousSiblingId: "o-doing",
        nextSiblingId: null,
      },
      expect.objectContaining({
        idempotencyKey: expect.stringMatching(/^move-field-option:/),
      }),
    );
    // The array keeps its order; the swapped keys are what re-orders the lanes.
    expect(during).toEqual(["Cần làm", "Đang làm"]);
    const moved = queryClient
      .getQueryData<FullBoardResponse>(key)!
      .board.fieldDefinitions[0]!.options.find((o) => o.id === "o-todo")!;
    expect(moved.position).toBe("a1");
  });

  it("sends nothing when the option is already first or last", async () => {
    const { queryClient } = seeded();
    const writer: FieldOptionWriter = {
      addOption: vi.fn(),
      updateOption: vi.fn(),
      removeOption: vi.fn(),
      moveOption: vi.fn(),
    };
    await runMoveFieldOption(
      queryClient,
      writer,
      { ...target, optionId: "o-todo" },
      "before",
    );
    await runMoveFieldOption(
      queryClient,
      writer,
      { ...target, optionId: "o-doing" },
      "after",
    );
    expect(writer.moveOption).not.toHaveBeenCalled();
  });
});

function card(id: string, statusOptionId: string): Card {
  return {
    id,
    listId: "g-1",
    boardId: "b-1",
    workspaceId: "ws-1",
    title: id,
    position: 1,
    status: "",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: { [FIELD]: statusOptionId },
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
    statusOptionId,
  };
}
