import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import {
  groupCardsByStatus,
  type GeneratedBoardAccessDto,
  type GeneratedFullBoardDto,
} from "@notrelix/work-management-core";
import {
  createBoardAccessApi,
  createBoardApi,
  createCardApi,
} from "../../../../product/work-management/state/src/index";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;
const boardId = mockIds.boards.erp;

function clientFor(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

describe("ERP board in the mock world", () => {
  it("serves /full in the generated contract shape", async () => {
    const { client } = clientFor();
    const dto = await client.api.get<GeneratedFullBoardDto>(
      endpoints.boards.full(boardId),
    );
    expect(dto).toMatchObject({
      id: boardId,
      workspaceId,
      title: "Triển khai ERP Q4",
      itemKeyPrefix: "ERP",
      spaceId: mockIds.spaces.engineering,
    });
    expect(typeof dto.version).toBe("number");
    const list = dto.lists![0]!;
    expect(list).not.toHaveProperty("cards");
    const item = list.boardItems!.find((i) => i.itemKey === "ERP-142")!;
    expect(item).toMatchObject({
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      parentItemId: null,
      dueAt: "2026-10-10T10:00:00.000Z",
      childProgress: { done: 3, total: 5 },
    });
    expect(typeof item.position).toBe("string");
    expect(item).not.toHaveProperty("title");
    const status = dto.columns!.find((c) => c.fieldType === "Status")!;
    expect(status.options!.map((o) => [o.name, o.category])).toEqual([
      ["Cần làm", "todo"],
      ["Đang làm", "in_progress"],
      ["Chờ duyệt", "review"],
      ["Hoàn thành", "done"],
    ]);
  });

  it("maps into lanes 6/3/2/5 with keys, priorities and assignees", async () => {
    const { client } = clientFor();
    const board = await createBoardApi(client).getFullBoard(boardId, {
      workspaceId,
    });
    const lanes = groupCardsByStatus(board);
    expect(lanes.map((lane) => [lane.name, lane.total])).toEqual([
      ["Cần làm", 6],
      ["Đang làm", 3],
      ["Chờ duyệt", 2],
      ["Hoàn thành", 5],
    ]);
    const erp142 = lanes[1]!.cards[0]!;
    expect(erp142).toMatchObject({
      itemKey: "ERP-142",
      priority: "urgent",
      labels: [expect.objectContaining({ name: "Dữ liệu" })],
    });
    expect(erp142.members.map((m) => m.name)).toEqual(["Trần Minh Quân"]);
    const priorities = lanes
      .flatMap((lane) => lane.cards)
      .reduce<Record<string, number>>((acc, card) => {
        acc[card.priority ?? "none"] = (acc[card.priority ?? "none"] ?? 0) + 1;
        return acc;
      }, {});
    expect(priorities).toEqual({ urgent: 1, high: 5, medium: 7, low: 3 });
  });

  it("persists a status written through the item's field value", async () => {
    const { client } = clientFor();
    const boards = createBoardApi(client);
    const before = await boards.getFullBoard(boardId, { workspaceId });
    const statusField = before.fieldDefinitions.find(
      (field) => field.semanticRole === "status",
    )!;
    const doing = groupCardsByStatus(before)[1]!;
    const erp149 = groupCardsByStatus(before)[0]!.cards.find(
      (card) => card.itemKey === "ERP-149",
    )!;

    await createCardApi(client).updateFieldValue({
      cardId: erp149.id,
      fieldDefinitionId: statusField.id,
      value: doing.optionId,
    });

    const after = groupCardsByStatus(
      await boards.getFullBoard(boardId, { workspaceId }),
    );
    expect(after.map((lane) => lane.total)).toEqual([5, 4, 2, 5]);
    expect(after[1]!.cards.map((card) => card.itemKey)).toContain("ERP-149");
  });

  it("answers /access with the owner's level and the board's audience", async () => {
    const { client } = clientFor("owner");
    const access = await createBoardAccessApi(client).getBoardAccess(boardId);
    expect(access).toMatchObject({
      boardId,
      myLevel: "manager",
      mySource: { kind: "owner", grantedBy: null },
      peopleCount: 32,
      guestCount: 1,
      peopleCountComplete: false,
    });
    expect(access.sources.find((s) => s.kind === "space")).toMatchObject({
      id: mockIds.spaces.engineering,
      name: "Engineering",
    });
  });

  it("gives the commenter persona a commenter grant and hides names she cannot see", async () => {
    const { client, store } = clientFor("commenter");
    expect(store.getCurrentUser().name).toBe("Đỗ Khánh Linh");
    const dto = await client.api.get<GeneratedBoardAccessDto>(
      endpoints.boards.access(boardId),
    );
    expect(dto).toMatchObject({
      myLevel: "commenter",
      mySource: {
        kind: "explicit",
        grantedBy: { name: "Lê Hoàng Phúc" },
      },
    });
    expect(
      dto.sources!.every((source) => source.id == null && source.name == null),
    ).toBe(true);
    // The Engineering Space is Private and she is not in it.
    const full = await client.api.get<GeneratedFullBoardDto>(
      endpoints.boards.full(boardId),
    );
    expect(full.spaceId).toBeNull();
  });

  it("answers NotFound for a board that does not exist", async () => {
    const { client } = clientFor();
    await expect(
      client.api.get(endpoints.boards.access("missing-board")),
    ).rejects.toThrow();
  });

  it("keeps the default world's member list and roadmap board", async () => {
    const { store, client } = clientFor("owner");
    expect(store.getWorkspaceMembers(workspaceId)).toHaveLength(4);
    const roadmap = await createBoardApi(client).getFullBoard(
      mockIds.boards.roadmap,
      { workspaceId },
    );
    const cards = roadmap.groups.flatMap((group) => group.cards);
    expect(cards.map((card) => card.title)).toContain("Ship mock runtime");
    const lanes = groupCardsByStatus(roadmap);
    expect(lanes.map((lane) => lane.name)).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ]);
  });

  it("writes an item's status through PATCH /board-items/{id}/status and marks Done items completed", async () => {
    const { client, store } = clientFor();
    const cards = createCardApi(client);
    const boards = createBoardApi(client);
    const itemId = (key: string) =>
      store
        .getLists(boardId)
        .flatMap((list) => store.getCards(list.id))
        .find((card) => card.itemKey === key)!.id;
    const lanesOf = async () => {
      const full = await boards.getFullBoard(boardId, { workspaceId });
      return groupCardsByStatus({
        fieldDefinitions: full.board.fieldDefinitions,
        groups: full.groups,
      });
    };

    await cards.updateStatus(itemId("ERP-149"), "mock-erp-status-doing");
    let lanes = await lanesOf();
    expect(lanes.map((lane) => lane.total)).toEqual([5, 4, 2, 5]);
    expect(lanes[1]!.cards.some((card) => card.itemKey === "ERP-149")).toBe(
      true,
    );
    expect(store.getCard(itemId("ERP-149"))!.completedAt ?? null).toBeNull();

    await cards.updateStatus(itemId("ERP-149"), "mock-erp-status-done");
    expect(store.getCard(itemId("ERP-149"))!.completedAt).toEqual(
      expect.any(String),
    );
    await cards.updateStatus(itemId("ERP-149"), "mock-erp-status-todo");
    expect(store.getCard(itemId("ERP-149"))!.completedAt ?? null).toBeNull();
    lanes = await lanesOf();
    expect(lanes.map((lane) => lane.total)).toEqual([6, 3, 2, 5]);
  });

  it("rejects a status that is not an option of the board's Status field", async () => {
    const { client, store } = clientFor();
    const card = store.getCards(store.getLists(boardId)[0]!.id)[0]!;
    await expect(
      createCardApi(client).updateStatus(card.id, "not-an-option"),
    ).rejects.toThrow();
  });

  it("returns the created item so a lane can set its status next", async () => {
    const { client, store } = clientFor();
    const groupId = store.getLists(boardId)[0]!.id;
    const id = await createCardApi(client).createCard(boardId, {
      listId: groupId,
      title: "Rà soát quyền kế toán",
    });
    expect(store.getCard(id!)).toMatchObject({
      title: "Rà soát quyền kế toán",
      itemKey: "ERP-154",
    });
  });
});
