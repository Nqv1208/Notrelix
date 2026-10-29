import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { projectComputedValues } from "../state/mock-computed-values";
import {
  ERP_DOUBLE_COLUMN_ID,
  ERP_LINK_COLUMN_ID,
  ERP_NOTE_COLUMN_ID,
  ERP_POINTS_COLUMN_ID,
} from "../state/mock-item-detail";
import { erpItemId } from "../state/mock-erp-board";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const BOARD = "mock-board-erp";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

interface FullBoard {
  columns: { id: string; fieldType: string }[];
  lists: {
    boardItems: {
      id: string;
      fieldValues?: Record<string, unknown>;
      computedValues?: Record<string, { state: string; value?: unknown }>;
    }[];
  }[];
}

async function readItem(
  client: ReturnType<typeof setup>["client"],
  key: string,
) {
  const board = await client.api.get<FullBoard>(`/boards/${BOARD}/full`);
  const item = board.lists
    .flatMap((group) => group.boardItems)
    .find((entry) => entry.id === erpItemId(key));
  return { board, item: item! };
}

describe("custom field values in the mock board read", () => {
  it("names the new field types and carries fieldValues and computedValues per item", async () => {
    const { client } = setup();
    const { board, item } = await readItem(client, "ERP-144");
    const type = (id: string) =>
      board.columns.find((column) => column.id === id)?.fieldType;
    expect(type(ERP_NOTE_COLUMN_ID)).toBe("LongText");
    expect(type(ERP_LINK_COLUMN_ID)).toBe("Link");
    expect(type(ERP_POINTS_COLUMN_ID)).toBe("Number");
    expect(type(ERP_DOUBLE_COLUMN_ID)).toBe("Formula");
    expect(item.fieldValues?.[ERP_POINTS_COLUMN_ID]).toBe(13);
    expect(item.fieldValues?.[ERP_LINK_COLUMN_ID]).toBe(
      "https://erp.minhan.vn/t/144",
    );
    expect(item.computedValues?.[ERP_DOUBLE_COLUMN_ID]).toEqual({
      state: "ok",
      value: 26,
    });
  });

  it("an item without the source value has an empty formula result", async () => {
    const { client } = setup();
    const { item } = await readItem(client, "ERP-142");
    expect(item.computedValues?.[ERP_DOUBLE_COLUMN_ID]).toEqual({
      state: "empty",
    });
  });

  it("writes a value and reads the formula again from it", async () => {
    const { client } = setup();
    await client.api.patch(
      `/board-items/${erpItemId("ERP-142")}/values/${ERP_POINTS_COLUMN_ID}`,
      { value: 21 },
    );
    const { item } = await readItem(client, "ERP-142");
    expect(item.computedValues?.[ERP_DOUBLE_COLUMN_ID]).toEqual({
      state: "ok",
      value: 42,
    });
  });

  it("refuses to write a formula", async () => {
    const { client } = setup();
    await expect(
      client.api.patch(
        `/board-items/${erpItemId("ERP-142")}/values/${ERP_DOUBLE_COLUMN_ID}`,
        { value: 1 },
      ),
    ).rejects.toBeDefined();
  });

  it("a custom Person-free write does not move the assignees", () => {
    const { store } = setup();
    const before = store.getCard(erpItemId("ERP-144"))?.assigneeUserIds;
    store.updateFieldValue(erpItemId("ERP-144"), ERP_NOTE_COLUMN_ID, "x");
    expect(store.getCard(erpItemId("ERP-144"))?.assigneeUserIds).toEqual(
      before,
    );
    expect(mockIds.workspaces.primary).toBeTruthy();
  });
});

describe("projectComputedValues", () => {
  const columns = [
    { id: "n", name: "Số", fieldType: "number" },
    {
      id: "f",
      name: "F",
      fieldType: "formula",
      settings: JSON.stringify({ expression: "([Số] + 1) * 2 / [Số]" }),
    },
  ];

  it("evaluates arithmetic over referenced values", () => {
    expect(projectComputedValues(columns, { n: 2 })).toEqual({
      f: { state: "ok", value: 3 },
    });
  });

  it("reports divide-by-zero, an empty source and a wrong type", () => {
    expect(projectComputedValues(columns, { n: 0 }).f).toEqual({
      state: "error",
      errorCode: "divide-by-zero",
    });
    expect(projectComputedValues(columns, {}).f).toEqual({ state: "empty" });
    expect(projectComputedValues(columns, { n: "a" }).f).toEqual({
      state: "error",
      errorCode: "type",
    });
  });

  it("rejects an expression that is not arithmetic", () => {
    const bad = [
      {
        id: "f",
        name: "F",
        fieldType: "formula",
        settings: '{"expression":"1 +"}',
      },
    ];
    expect(projectComputedValues(bad, {}).f).toEqual({
      state: "error",
      errorCode: "invalid-definition",
    });
  });
});
