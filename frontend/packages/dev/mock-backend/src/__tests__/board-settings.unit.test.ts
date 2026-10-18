import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import type { BoardSettingsDto } from "@notrelix/work-management-core";
import { createBoardSettingsApi } from "../../../../product/work-management/state/src/index";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

const boardId = mockIds.boards.erp;

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  const api = createBoardSettingsApi(client);
  const read = () => api.get(boardId);
  return { store, api, read };
}

const field = (dto: BoardSettingsDto, id: string) =>
  dto.fields.find((entry) => entry.id === id)!;

describe("board settings in the mock world (CTR-GAP-WM-BOARD-SETTINGS)", () => {
  it("reads the ERP board as the settings mockup lists it", async () => {
    const dto = await setup().read();
    expect(dto.fields).toHaveLength(11);
    expect(dto.systemFields).toHaveLength(5);
    expect(dto.archivedFields.map((entry) => entry.key)).toEqual([
      "handover_date",
    ]);
    expect(dto.views).toHaveLength(7);
    expect(dto.groups.map((group) => [group.name, group.itemCount])).toEqual([
      ["Chuẩn bị", 6],
      ["Dữ liệu", 3],
      ["Kiểm thử", 2],
      ["Đào tạo và vận hành", 5],
    ]);
    expect(dto.itemCount).toBe(16);
    expect(dto.automations.filter((rule) => rule.enabled)).toHaveLength(3);
    expect(dto.general).toMatchObject({
      title: "Triển khai ERP Q4",
      itemKeyPrefix: "ERP",
      sampleItemKey: "ERP-142",
      timeZone: "Asia/Ho_Chi_Minh",
    });
  });

  it("counts the items of each priority option from the items themselves", async () => {
    const dto = await setup().read();
    const priority = field(dto, "sf-priority");
    expect(
      priority.options.map((option) => [option.name, option.itemCount]),
    ).toEqual([
      ["Khẩn", 1],
      ["Cao", 5],
      ["Trung bình", 7],
      ["Thấp", 3],
    ]);
    expect(priority.valueCount).toBe(16);
    expect(priority.detail).toBe("4 lựa chọn");
  });

  it("only a manager of the board reads or writes the settings", async () => {
    for (const persona of ["commenter", "viewer"] as const) {
      const { api } = setup(persona);
      await expect(api.get(boardId)).rejects.toMatchObject({
        kind: "forbidden",
      });
      await expect(
        api.updateField(boardId, 1, "sf-priority", { name: "X" }),
      ).rejects.toMatchObject({ kind: "forbidden" });
    }
  });

  it("renames a field through its column and refuses a name in use", async () => {
    const { api, read, store } = setup();
    const first = await read();
    await api.updateField(boardId, first.version, "sf-priority", {
      name: "Mức ưu tiên",
    });
    const after = await read();
    expect(field(after, "sf-priority").name).toBe("Mức ưu tiên");
    expect(after.version).toBe(first.version + 1);
    expect(
      store.getColumns(boardId).some((column) => column.name === "Mức ưu tiên"),
    ).toBe(true);
    await expect(
      api.updateField(boardId, after.version, "sf-owner", {
        name: "mức ưu tiên",
      }),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("answers 409 to a write made from a stale read", async () => {
    const { api, read } = setup();
    const first = await read();
    await api.updateField(boardId, first.version, "sf-priority", {
      guidance: "a",
    });
    await expect(
      api.updateField(boardId, first.version, "sf-priority", { guidance: "b" }),
    ).rejects.toMatchObject({ kind: "conflict" });
    expect(field(await read(), "sf-priority").guidance).toBe("a");
  });

  it("adds, recolours, reorders and deletes options of the column", async () => {
    const { api, read, store } = setup();
    let dto = await read();
    await api.addOption(boardId, dto.version, "sf-priority", {
      name: "Chưa xác định",
      color: "#1E7BE0",
    });
    dto = await read();
    expect(field(dto, "sf-priority").options.at(-1)).toMatchObject({
      name: "Chưa xác định",
      itemCount: 0,
    });
    await expect(
      api.addOption(boardId, dto.version, "sf-priority", {
        name: "KHẨN",
        color: "#000000",
      }),
    ).rejects.toMatchObject({ kind: "validation" });

    const ids = field(dto, "sf-priority").options.map((option) => option.id);
    await api.reorderOptions(
      boardId,
      dto.version,
      "sf-priority",
      [...ids].reverse(),
    );
    dto = await read();
    expect(
      field(dto, "sf-priority").options.map((option) => option.id),
    ).toEqual([...ids].reverse());

    await api.updateOption(
      boardId,
      dto.version,
      "sf-priority",
      "mock-erp-priority-low",
      { color: "#2C5E4F" },
    );
    dto = await read();
    expect(
      field(dto, "sf-priority").options.find(
        (option) => option.id === "mock-erp-priority-low",
      )?.color,
    ).toBe("#2C5E4F");

    await api.deleteOption(
      boardId,
      dto.version,
      "sf-priority",
      "mock-erp-priority-low",
    );
    expect(
      store
        .getColumn("mock-erp-col-priority")!
        .options!.some((option) => option.id === "mock-erp-priority-low"),
    ).toBe(false);
  });

  it("archives a field out of the board and restores it with its values", async () => {
    const { api, read, store } = setup();
    let dto = await read();
    await api.archiveField(boardId, dto.version, "sf-priority");
    dto = await read();
    expect(dto.fields.some((entry) => entry.id === "sf-priority")).toBe(false);
    expect(dto.archivedFields.map((entry) => entry.key)).toContain("priority");
    expect(store.getColumn("mock-erp-col-priority")!.isHidden).toBe(true);
    await api.restoreField(boardId, dto.version, "sf-priority");
    dto = await read();
    expect(field(dto, "sf-priority").valueCount).toBe(16);
    expect(store.getColumn("mock-erp-col-priority")!.isHidden).toBe(false);
  });

  it("does not archive the Status field", async () => {
    const { api, read } = setup();
    const dto = await read();
    await expect(
      api.archiveField(boardId, dto.version, "sf-status"),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("adds a field of a column type with the next free key", async () => {
    const { api, read, store } = setup();
    const before = await read();
    await api.createField(boardId, before.version, "number");
    const after = await read();
    expect(after.fields).toHaveLength(12);
    const created = after.fields.at(-1)!;
    expect(created).toMatchObject({ name: "Trường mới", type: "number" });
    expect(store.getColumn(created.id.replace("sf-", ""))).toBeDefined();
    expect(new Set(after.fields.map((entry) => entry.key)).size).toBe(12);
  });

  it("reorders the board fields and refuses an incomplete order", async () => {
    const { api, read } = setup();
    const dto = await read();
    const ids = dto.fields.map((entry) => entry.id);
    await api.reorderFields(boardId, dto.version, [...ids].reverse());
    const after = await read();
    expect(after.fields.map((entry) => entry.id)).toEqual([...ids].reverse());
    await expect(
      api.reorderFields(boardId, after.version, ids.slice(1)),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("marks the views that place by an archived field and fixes them", async () => {
    const { api, read } = setup();
    let dto = await read();
    expect(
      dto.views.filter((view) => view.needsFix).map((view) => view.id),
    ).toEqual(["sv-handover"]);
    await api.updateView(boardId, dto.version, "sv-handover", {
      fieldId: "sf-due",
    });
    dto = await read();
    expect(dto.views.some((view) => view.needsFix)).toBe(false);
    expect(dto.views.find((view) => view.id === "sv-handover")?.summary).toBe(
      "Theo Hạn chót",
    );
  });

  it("creates, duplicates, reorders and deletes saved views; the default stays", async () => {
    const { api, read } = setup();
    let dto = await read();
    await api.createView(boardId, dto.version, "calendar");
    dto = await read();
    const created = dto.views.at(-1)!;
    expect(created).toMatchObject({
      name: "Chế độ xem mới",
      kind: "calendar",
      visibility: "me",
    });
    await api.duplicateView(boardId, dto.version, created.id);
    dto = await read();
    expect(dto.views.map((view) => view.name)).toContain(
      "Chế độ xem mới (bản sao)",
    );
    await api.reorderViews(
      boardId,
      dto.version,
      [...dto.views.map((view) => view.id)].reverse(),
    );
    dto = await read();
    expect(dto.views[0]!.name).toBe("Chế độ xem mới (bản sao)");
    await api.deleteView(boardId, dto.version, created.id);
    dto = await read();
    expect(dto.views.some((view) => view.id === created.id)).toBe(false);
    await expect(
      api.deleteView(boardId, dto.version, "sv-table"),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("deletes a group by moving its items first, never the last group", async () => {
    const { api, read, store } = setup();
    let dto = await read();
    const test = dto.groups.find((group) => group.name === "Kiểm thử")!;
    const data = dto.groups.find((group) => group.name === "Dữ liệu")!;
    await expect(
      api.deleteGroup(boardId, dto.version, test.id, null),
    ).rejects.toMatchObject({ kind: "validation" });
    await api.deleteGroup(boardId, dto.version, test.id, data.id);
    dto = await read();
    expect(dto.groups.map((group) => [group.name, group.itemCount])).toEqual([
      ["Chuẩn bị", 6],
      ["Dữ liệu", 5],
      ["Đào tạo và vận hành", 5],
    ]);
    expect(dto.itemCount).toBe(16);
    expect(store.getLists(boardId)).toHaveLength(3);
  });

  it("adds, renames and reorders groups", async () => {
    const { api, read } = setup();
    let dto = await read();
    await api.createGroup(boardId, dto.version, "Nghiệm thu");
    dto = await read();
    expect(dto.groups.at(-1)).toMatchObject({
      name: "Nghiệm thu",
      itemCount: 0,
    });
    await api.updateGroup(boardId, dto.version, dto.groups[0]!.id, {
      name: "Khởi động",
    });
    dto = await read();
    expect(dto.groups[0]!.name).toBe("Khởi động");
    await api.reorderGroups(
      boardId,
      dto.version,
      [...dto.groups.map((group) => group.id)].reverse(),
    );
    dto = await read();
    expect(dto.groups[0]!.name).toBe("Nghiệm thu");
  });

  it("turns a rule on and off, and keeps a rule with no runner off", async () => {
    const { api, read } = setup();
    let dto = await read();
    await api.setAutomationEnabled(boardId, dto.version, "sa-overdue", false);
    dto = await read();
    expect(
      dto.automations.find((rule) => rule.id === "sa-overdue")?.enabled,
    ).toBe(false);
    await expect(
      api.setAutomationEnabled(boardId, dto.version, "sa-warehouse", true),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("changes the general settings on the board itself", async () => {
    const { api, read, store } = setup();
    const dto = await read();
    await api.updateGeneral(boardId, dto.version, {
      title: "Triển khai ERP Q1",
      itemKeyPrefix: "erq",
      color: "#5B5BD6",
      timeZone: "Asia/Tokyo",
      weekStart: "sunday",
    });
    const after = await read();
    expect(after.general).toMatchObject({
      title: "Triển khai ERP Q1",
      itemKeyPrefix: "ERQ",
      color: "#5B5BD6",
      timeZone: "Asia/Tokyo",
      weekStart: "sunday",
    });
    expect(store.getBoard(boardId)?.title).toBe("Triển khai ERP Q1");
    await expect(
      api.updateGeneral(boardId, after.version, { itemKeyPrefix: "12" }),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("archives the board, and deletes it only when the title is typed exactly", async () => {
    const { api, read, store } = setup();
    let dto = await read();
    await api.archiveBoard(boardId, dto.version);
    expect(store.getBoard(boardId)?.isArchived).toBe(true);
    dto = await read();
    await expect(
      api.deleteBoard(boardId, dto.version, "triển khai erp q4"),
    ).rejects.toMatchObject({ kind: "validation" });
    await api.deleteBoard(boardId, dto.version, "Triển khai ERP Q4");
    expect(store.getBoard(boardId)).toBeUndefined();
    expect(store.getLists(boardId)).toEqual([]);
  });
});
