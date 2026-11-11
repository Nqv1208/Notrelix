import { describe, expect, it, vi } from "vitest";
import { sendNewChecklistItem } from "./send-new-checklist-item";

const list = (version: number, titles: string[]) => ({
  id: "list-1",
  title: "L",
  version,
  items: titles.map((title, i) => ({
    id: `item-${i}`,
    title,
    isDone: false,
    position: i,
  })),
});

function fakeApi(overrides: Record<string, unknown> = {}) {
  const calls: string[] = [];
  let version = 5;
  const titles = ["old"];
  return {
    calls,
    api: {
      createChecklistItem: vi.fn(async (input: { title: string }) => {
        calls.push("create");
        titles.push(input.title);
        version += 1;
      }),
      getChecklists: vi.fn(async () => [list(version, [...titles])]),
      setChecklistItemAssignee: vi.fn(async () => {
        calls.push("assign");
        version += 1;
      }),
      setChecklistItemDue: vi.fn(async () => {
        calls.push("due");
        version += 1;
      }),
      ...overrides,
    },
  };
}

const BASE = {
  cardId: "card-1",
  checklistId: "list-1",
  text: "new",
  expectedVersion: 5,
};

describe("sendNewChecklistItem", () => {
  it("only creates when the item starts with no person and no date", async () => {
    const { api, calls } = fakeApi();
    await sendNewChecklistItem(api as never, {
      ...BASE,
      assigneeId: null,
      dueAt: null,
    });
    expect(calls).toEqual(["create"]);
    expect(api.getChecklists).not.toHaveBeenCalled();
  });

  it("gives the newest item with that title its person, then its date, each at the version the last write left", async () => {
    const { api, calls } = fakeApi();
    await sendNewChecklistItem(api as never, {
      ...BASE,
      assigneeId: "user-9",
      dueAt: "2026-11-11T10:00:00.000Z",
    });
    expect(calls).toEqual(["create", "assign", "due"]);
    expect(api.setChecklistItemAssignee).toHaveBeenCalledWith({
      itemId: "item-1",
      assigneeUserId: "user-9",
      expectedVersion: 6,
    });
    expect(api.setChecklistItemDue).toHaveBeenCalledWith({
      itemId: "item-1",
      dueAt: "2026-11-11T10:00:00.000Z",
      expectedVersion: 7,
    });
  });

  it("sets only the date when there is no person", async () => {
    const { api, calls } = fakeApi();
    await sendNewChecklistItem(api as never, {
      ...BASE,
      assigneeId: null,
      dueAt: "2026-11-11T10:00:00.000Z",
    });
    expect(calls).toEqual(["create", "due"]);
  });

  it("does not fail the write when the person is refused: the item exists, so a retry would add it twice", async () => {
    const { api } = fakeApi({
      setChecklistItemAssignee: vi.fn(async () => {
        throw new Error("412");
      }),
    });
    await expect(
      sendNewChecklistItem(api as never, {
        ...BASE,
        assigneeId: "user-9",
        dueAt: null,
      }),
    ).resolves.toBeUndefined();
  });

  it("fails the write when the create itself is refused", async () => {
    const { api } = fakeApi({
      createChecklistItem: vi.fn(async () => {
        throw new Error("500");
      }),
    });
    await expect(
      sendNewChecklistItem(api as never, {
        ...BASE,
        assigneeId: "user-9",
        dueAt: null,
      }),
    ).rejects.toThrow("500");
  });
});
