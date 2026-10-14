import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { groupCardsByStatus } from "@notrelix/work-management-core";
import {
  createBoardAccessApi,
  createBoardApi,
  createCardApi,
  type GeneratedBoardAccessDto,
  type GeneratedFullBoardDto,
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
    expect(dto.lists!.map((l) => l.title)).toEqual([
      "Chuẩn bị",
      "Dữ liệu",
      "Kiểm thử",
      "Đào tạo và vận hành",
    ]);
    for (const l of dto.lists!) expect(l).not.toHaveProperty("cards");
    const item = dto
      .lists!.flatMap((l) => l.boardItems ?? [])
      .find((i) => i.itemKey === "ERP-142")!;
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

  describe("writes behave like the real API", () => {
    const BASE = "http://localhost:8000/api/v1";
    function rawFetch(persona: MockPersona = "owner") {
      const store = new MockStore({ ...defaultConfig, persona });
      return { store, fetch: createMockFetch(store) };
    }
    function erpCard(store: MockStore, key: string) {
      return store
        .getLists(boardId)
        .flatMap((list) => store.getCards(list.id))
        .find((card) => card.itemKey === key)!;
    }
    const json = (body: unknown, key?: string): RequestInit => ({
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        ...(key ? { "Idempotency-Key": key } : {}),
      },
      body: JSON.stringify(body),
    });

    it("status: 204 with a key, 400 without one, 403 for a commenter", async () => {
      const owner = rawFetch();
      const item = erpCard(owner.store, "ERP-149");
      const url = `${BASE}/board-items/${item.id}/status`;
      const noKey = await owner.fetch(
        url,
        json({ status: "mock-erp-status-doing" }),
      );
      expect(noKey.status).toBe(400);
      const done = await owner.fetch(
        url,
        json({ status: "mock-erp-status-doing" }, "status-key-1"),
      );
      expect(done.status).toBe(204);

      const commenter = rawFetch("commenter");
      const theirs = erpCard(commenter.store, "ERP-149");
      const denied = await commenter.fetch(
        `${BASE}/board-items/${theirs.id}/status`,
        json({ status: "mock-erp-status-doing" }, "status-key-2"),
      );
      expect(denied.status).toBe(403);
    });

    it("a viewer (a guest with a view entry) reads the board and cannot write", async () => {
      const { client, store } = clientFor("viewer");
      const access = await createBoardAccessApi(client).getBoardAccess(boardId);
      expect(access.myLevel).toBe("viewer");
      const item = erpCard(store, "ERP-149");
      await expect(
        createCardApi(client).updateStatus(item.id, "mock-erp-status-doing"),
      ).rejects.toMatchObject({ kind: "forbidden" });
    });

    it("a commenter's status write is refused and changes nothing", async () => {
      const { client, store } = clientFor("commenter");
      const item = erpCard(store, "ERP-149");
      const before = store.getFieldValues(item.id);
      await expect(
        createCardApi(client).updateStatus(item.id, "mock-erp-status-doing"),
      ).rejects.toMatchObject({ kind: "forbidden" });
      expect(store.getFieldValues(item.id)).toEqual(before);
    });

    it("move: keeps the order given by the neighbours, checks the version and answers with the new one", async () => {
      const { client, store } = clientFor();
      const cards = createCardApi(client);
      const group = store.getLists(boardId)[0]!;
      const [first, second, third] = store.getCards(group.id);
      const moved = await cards.moveCard({
        cardId: third!.id,
        listId: group.id,
        expectedVersion: 1,
        previousSiblingId: first!.id,
        nextSiblingId: second!.id,
      });
      expect(moved.version).toBe(2);
      expect(
        store
          .getCards(group.id)
          .slice(0, 3)
          .map((card) => card.id),
      ).toEqual([first!.id, third!.id, second!.id]);
      // The same item version again is stale now.
      await expect(
        cards.moveCard({
          cardId: third!.id,
          listId: group.id,
          expectedVersion: 1,
          previousSiblingId: null,
          nextSiblingId: first!.id,
        }),
      ).rejects.toBeTruthy();
    });

    it("move: 400 without a version or a key, 403 for a commenter", async () => {
      const owner = rawFetch();
      const group = owner.store.getLists(boardId)[0]!;
      const item = owner.store.getCards(group.id)[0]!;
      const post = (body: unknown, key?: string): RequestInit => ({
        ...json(body, key),
        method: "POST",
      });
      const url = `${BASE}/board-items/${item.id}/move`;
      expect(
        (await owner.fetch(url, post({ groupId: group.id }, "move-key-1")))
          .status,
      ).toBe(400);
      expect(
        (
          await owner.fetch(
            url,
            post({ groupId: group.id, expectedVersion: 1 }),
          )
        ).status,
      ).toBe(400);
      const commenter = rawFetch("commenter");
      const theirGroup = commenter.store.getLists(boardId)[0]!;
      const theirs = commenter.store.getCards(theirGroup.id)[0]!;
      const denied = await commenter.fetch(
        `${BASE}/board-items/${theirs.id}/move`,
        post({ groupId: theirGroup.id, expectedVersion: 1 }, "move-key-2"),
      );
      expect(denied.status).toBe(403);
    });

    it("archive: the item leaves the board read; a commenter cannot archive", async () => {
      const { client, store } = clientFor();
      const item = erpCard(store, "ERP-149");
      await createCardApi(client).archiveCard(item.id);
      const full = await createBoardApi(client).getFullBoard(boardId, {
        workspaceId,
      });
      const lanes = groupCardsByStatus({
        fieldDefinitions: full.board.fieldDefinitions,
        groups: full.groups,
      });
      expect(lanes.map((lane) => lane.total)).toEqual([5, 3, 2, 5]);

      const commenter = clientFor("commenter");
      const theirs = erpCard(commenter.store, "ERP-151");
      await expect(
        createCardApi(commenter.client).archiveCard(theirs.id),
      ).rejects.toBeTruthy();
    });
  });
});
