import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { groupCardsByStatus } from "@notrelix/work-management-core";
import {
  createBoardAccessApi,
  createBoardApi,
  createCardApi,
  createItemExtrasApi,
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

/** The versions of the named items as the store holds them: what a bulk write names. */
const seen = (store: MockStore, ids: readonly string[]) =>
  Object.fromEntries(ids.map((id) => [id, store.getCard(id)?.version ?? 1]));

describe("ERP board in the mock world", () => {
  it("serves the item detail with its assignee, priority option and description", async () => {
    const { client, store } = clientFor();
    const item = store.getCard("mock-erp-item-142")!;
    const card = await createCardApi(client).getCard(item.id);
    expect(card.members.map((member) => member.name)).toEqual([
      "Trần Minh Quân",
    ]);
    expect(card.priorityOptionId).toBe("mock-erp-priority-urgent");
    // The read carries no priority word, so none is invented.
    expect(card.priority).toBeUndefined();
    expect(card.descriptionMd).toContain("48.200 hồ sơ khách hàng");
    expect(card._count.comments).toBe(4);
  });

  it("an item without an assignee has no members in its detail", async () => {
    const { client, store } = clientFor();
    const item = store.getCard("mock-erp-item-153")!;
    const card = await createCardApi(client).getCard(item.id);
    expect(card.members).toEqual([]);
  });

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
    // Live Items holding each option, sub-items included (the lanes show 6/3/2/5 root items).
    expect(status.options!.map((o) => o.itemCount)).toEqual([8, 3, 2, 8]);
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

    await createCardApi(client).updateFieldValue(
      {
        cardId: erp149.id,
        fieldDefinitionId: statusField.id,
        value: doing.optionId,
      },
      { expectedVersion: erp149.version ?? 1 },
    );

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
      name: "Công nghệ",
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

    await cards.updateStatus(
      itemId("ERP-149"),
      "mock-erp-status-doing",
      store.getCard(itemId("ERP-149"))!.version ?? 1,
    );
    let lanes = await lanesOf();
    expect(lanes.map((lane) => lane.total)).toEqual([5, 4, 2, 5]);
    expect(lanes[1]!.cards.some((card) => card.itemKey === "ERP-149")).toBe(
      true,
    );
    expect(store.getCard(itemId("ERP-149"))!.completedAt ?? null).toBeNull();

    await cards.updateStatus(
      itemId("ERP-149"),
      "mock-erp-status-done",
      store.getCard(itemId("ERP-149"))!.version ?? 1,
    );
    expect(store.getCard(itemId("ERP-149"))!.completedAt).toEqual(
      expect.any(String),
    );
    await cards.updateStatus(
      itemId("ERP-149"),
      "mock-erp-status-todo",
      store.getCard(itemId("ERP-149"))!.version ?? 1,
    );
    expect(store.getCard(itemId("ERP-149"))!.completedAt ?? null).toBeNull();
    lanes = await lanesOf();
    expect(lanes.map((lane) => lane.total)).toEqual([6, 3, 2, 5]);
  });

  it("rejects a status that is not an option of the board's Status field", async () => {
    const { client, store } = clientFor();
    const card = store.getCards(store.getLists(boardId)[0]!.id)[0]!;
    await expect(
      createCardApi(client).updateStatus(
        card.id,
        "not-an-option",
        card.version ?? 1,
      ),
    ).rejects.toThrow();
  });

  it("returns the created item so a lane can set its status next", async () => {
    const { client, store } = clientFor();
    const groupId = store.getLists(boardId)[0]!.id;
    const created = await createCardApi(client).createCard(boardId, {
      listId: groupId,
      title: "Rà soát quyền kế toán",
    });
    expect(store.getCard(created!.id)).toMatchObject({
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

    it("status: 200 with the new version when sent with a key, 400 without one, 403 for a commenter", async () => {
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
      // The write answers the item's new version, the one the next write names.
      expect(done.status).toBe(200);
      expect(await done.json()).toEqual({ version: expect.any(Number) });

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
        createCardApi(client).updateStatus(
          item.id,
          "mock-erp-status-doing",
          item.version ?? 1,
        ),
      ).rejects.toMatchObject({ kind: "forbidden" });
    });

    it("a commenter's status write is refused and changes nothing", async () => {
      const { client, store } = clientFor("commenter");
      const item = erpCard(store, "ERP-149");
      const before = store.getFieldValues(item.id);
      await expect(
        createCardApi(client).updateStatus(
          item.id,
          "mock-erp-status-doing",
          item.version ?? 1,
        ),
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
      await createCardApi(client).archiveCard(item.id, item.version ?? 1);
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
        createCardApi(commenter.client).archiveCard(
          theirs.id,
          theirs.version ?? 1,
        ),
      ).rejects.toBeTruthy();
    });

    it("a person field value sets the assignees, and the duplicate keeps them", async () => {
      const { client, store } = clientFor();
      const item = erpCard(store, "ERP-153");
      const person = store
        .getColumns(boardId)
        .find((column) => column.fieldType === "user")!;
      await createCardApi(client).updateFieldValue(
        {
          cardId: item.id,
          fieldDefinitionId: person.id,
          value: [mockIds.users.owner],
        },
        { expectedVersion: item.version ?? 1 },
      );
      expect(store.getCard(item.id)!.assigneeUserIds).toEqual([
        mockIds.users.owner,
      ]);
      const copy = store.duplicateCard(item.id)!;
      expect(store.getCard(copy.id)!.assigneeUserIds).toEqual([
        mockIds.users.owner,
      ]);
    });

    it("move to board: the item and its children leave this board, take a new key and land in the group of the same name", async () => {
      const { client, store } = clientFor();
      const item = erpCard(store, "ERP-142");
      const target = store
        .getBoards(workspaceId)
        .find(
          (candidate) =>
            candidate.id !== boardId && store.getLists(candidate.id).length > 0,
        )!;
      await createItemExtrasApi(client).moveToBoard(
        item.id,
        target.id,
        item.version ?? 1,
      );
      const moved = store.getCard(item.id)!;
      expect(moved.boardId).toBe(target.id);
      expect(moved.itemKey).not.toBe("ERP-142");
      expect(
        store
          .getChildCards(item.id)
          .every((child) => child.boardId === target.id),
      ).toBe(true);
      const full = await createBoardApi(client).getFullBoard(boardId, {
        workspaceId,
      });
      expect(
        full.groups
          .flatMap((group) => group.cards)
          .some((c) => c.id === item.id),
      ).toBe(false);
    });

    it("move to board: a commenter cannot, and the same board is refused", async () => {
      const commenter = clientFor("commenter");
      const theirs = erpCard(commenter.store, "ERP-151");
      const other = commenter.store
        .getBoards(workspaceId)
        .find((candidate) => candidate.id !== boardId)!;
      await expect(
        createItemExtrasApi(commenter.client).moveToBoard(
          theirs.id,
          other.id,
          theirs.version ?? 1,
        ),
      ).rejects.toBeTruthy();
      const owner = clientFor();
      const item = erpCard(owner.store, "ERP-151");
      await expect(
        createItemExtrasApi(owner.client).moveToBoard(
          item.id,
          boardId,
          item.version ?? 1,
        ),
      ).rejects.toBeTruthy();
    });

    it("the plan read serves the phases, the milestone, the links and the completions", async () => {
      const { client } = clientFor();
      const plan = await createItemExtrasApi(client).getPlan(boardId);
      expect(plan.items.map((item) => [item.kind, item.title])).toEqual([
        ["phase", "UAT phân hệ bán hàng với phòng Kinh doanh"],
        ["phase", "Đào tạo phòng Kế toán"],
        ["phase", "Chạy song song hệ thống cũ và ERP"],
        ["milestone", "Go-live ERP"],
      ]);
      expect(plan.links).toHaveLength(7);
      expect(plan.links[0]).toEqual({
        from: "mock-erp-item-129",
        to: "mock-erp-item-142",
      });
      expect(plan.percents["mock-erp-item-138"]).toBe(90);
      expect(plan.groups["mock-erp-item-150"]).toBe("mock-erp-list-data");
    });

    it("the item detail: links, files, history, followers, custom values and the created line", async () => {
      const { client, store } = clientFor();
      const id = "mock-erp-item-142";
      const extras = createItemExtrasApi(client);
      const links = await extras.getLinks(id);
      expect(
        links.map((link) => [link.relation, link.item.key]).sort(),
      ).toEqual([
        ["blocked-by", "ERP-129"],
        ["blocks", "ERP-146"],
        ["blocks", "ERP-147"],
        ["related", "ERP-151"],
      ]);
      expect(
        links.find((link) => link.item.key === "ERP-129")?.item,
      ).toMatchObject({ statusName: "Hoàn thành", statusCategory: "done" });
      expect(store.getAttachments(id).map((file) => file.filename)).toEqual([
        "khach-hang-da-gop.xlsx",
        "cong-no-dau-ky-T9.xlsx",
        "quy-tac-gop-trung-v2.pdf",
      ]);
      const history = store.getActivity(id);
      expect(history).toHaveLength(13);
      expect(history[0]).toMatchObject({ kind: "created", verb: "tạo việc" });
      expect(store.getCardComments(id)).toHaveLength(4);
      const following = await extras.getFollowing(id);
      expect(following.followers.map((person) => person.name)).toEqual([
        "Nguyễn Thu Hà",
        "Trần Minh Quân",
        "Đỗ Khánh Linh",
        "Bùi Thanh Tâm",
      ]);
      expect(store.getFieldValues(id)["mock-erp-col-estimate"]).toBe(40);
      expect(store.getCard(id)?.createdAt).toBe("2026-09-23T08:10:00.000Z");
    });

    it("links, sub-items and files can be added and the history follows", async () => {
      const { client, store } = clientFor();
      const id = "mock-erp-item-142";
      const extras = createItemExtrasApi(client);
      await extras.addLink(id, {
        itemId: "mock-erp-item-150",
        relation: "blocked-by",
      });
      expect(store.getPlan("mock-board-erp").links).toContainEqual({
        from: "mock-erp-item-150",
        to: id,
      });
      await expect(
        extras.addLink(id, { itemId: id, relation: "related" }),
      ).rejects.toBeTruthy();
      await extras.addChildItem(id, "Kiểm tra lại");
      const children = await extras.getChildItems(id);
      expect(children).toHaveLength(6);
      expect(children[5]).toMatchObject({ key: "ERP-142.6", done: false });
      await extras.setChildDone(
        children[3]!.id,
        true,
        store.getCard(children[3]!.id)?.version ?? 1,
      );
      const last = store.getActivity(id).at(-1)!;
      expect(last).toMatchObject({
        verb: "đánh dấu xong việc con",
        target: "ERP-142.4",
      });
      await createCardApi(client).uploadCardFile(
        id,
        new File(["ghi chu"], "ghi-chu.pdf", { type: "application/pdf" }),
        { idempotencyKey: "erp-board-upload-1" },
      );
      expect(store.getAttachments(id)).toHaveLength(4);
      const commenter = clientFor("commenter");
      await expect(
        createItemExtrasApi(commenter.client).addChildItem(id, "x"),
      ).rejects.toBeTruthy();
    });
  });

  it("changes several items of the board in one bulk request, all or nothing", async () => {
    const { client, store } = clientFor();
    const api = createCardApi(client);
    const ids = ["mock-erp-item-142", "mock-erp-item-144"];

    const updated = await api.bulkUpdate(
      boardId,
      ids,
      {
        type: "set-due-date",
        dueDate: "2026-11-03T10:00:00.000Z",
      },
      seen(store, ids),
    );
    expect(updated).toBe(2);
    for (const id of ids) {
      expect(store.getCard(id)?.dueAt).toBe("2026-11-03T10:00:00.000Z");
    }

    await api.bulkUpdate(
      boardId,
      ids,
      { type: "clear-due-date" },
      seen(store, ids),
    );
    for (const id of ids) expect(store.getCard(id)?.dueAt ?? null).toBeNull();

    // An item that is not on the board stops the whole request: nothing is archived.
    await expect(
      api.bulkUpdate(
        boardId,
        [ids[0]!, "no-such-item"],
        { type: "archive" },
        seen(store, [ids[0]!, "no-such-item"]),
      ),
    ).rejects.toBeDefined();
    expect(store.getCard(ids[0]!)?.isArchived).toBeFalsy();

    await api.bulkUpdate(boardId, ids, { type: "archive" }, seen(store, ids));
    for (const id of ids) expect(store.getCard(id)?.isArchived).toBe(true);
  });

  it("sets the priority and deletes several items in one bulk request", async () => {
    const { client, store } = clientFor();
    const api = createCardApi(client);
    const ids = ["mock-erp-item-146", "mock-erp-item-147"];

    await api.bulkUpdate(
      boardId,
      ids,
      {
        type: "set-priority",
        priorityOptionId: "mock-erp-priority-urgent",
      },
      seen(store, ids),
    );
    for (const id of ids) {
      expect(store.getFieldValues(id)["mock-erp-col-priority"] ?? null).toBe(
        "mock-erp-priority-urgent",
      );
    }

    await expect(
      api.bulkUpdate(
        boardId,
        [ids[0]!, "no-such-item"],
        { type: "delete" },
        seen(store, [ids[0]!, "no-such-item"]),
      ),
    ).rejects.toBeDefined();
    expect(store.getCard(ids[0]!)).toBeDefined();

    await api.bulkUpdate(boardId, ids, { type: "delete" }, seen(store, ids));
    for (const id of ids) expect(store.getCard(id)).toBeUndefined();
  });
});
