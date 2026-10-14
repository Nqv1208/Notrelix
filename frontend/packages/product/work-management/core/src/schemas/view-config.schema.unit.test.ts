import { describe, expect, it } from "vitest";
import {
  DEFAULT_KANBAN_LANE_CARD_LIMIT,
  normalizeStatusManageRoles,
  resolveKanbanSettings,
  viewConfigSchema,
} from "./view-config.schema";

const OLD_CONFIG = {
  groupBy: "list",
  hiddenFields: [],
  columnOrder: [],
  columnWidths: { title: 320 },
  filters: [],
  sortBy: [],
};

describe("viewConfigSchema Kanban settings", () => {
  it("parses a config saved before the Kanban keys with today's behaviour", () => {
    const parsed = viewConfigSchema.parse(OLD_CONFIG);
    expect(parsed.laneCardLimit).toBe(DEFAULT_KANBAN_LANE_CARD_LIMIT);
    expect(parsed.statusManageRoles).toEqual(["manager", "editor"]);
    expect(parsed.keyboardMove).toBe(true);
    expect(parsed.columnWidths).toEqual({ title: 320 });
  });

  it("keeps the limit inside 1..20", () => {
    expect(
      viewConfigSchema.safeParse({ ...OLD_CONFIG, laneCardLimit: 0 }).success,
    ).toBe(false);
    expect(
      viewConfigSchema.safeParse({ ...OLD_CONFIG, laneCardLimit: 21 }).success,
    ).toBe(false);
    expect(
      viewConfigSchema.safeParse({ ...OLD_CONFIG, laneCardLimit: 2.5 }).success,
    ).toBe(false);
    expect(
      viewConfigSchema.parse({ ...OLD_CONFIG, laneCardLimit: 20 })
        .laneCardLimit,
    ).toBe(20);
  });

  it("always keeps manager in the status roles", () => {
    const parsed = viewConfigSchema.parse({
      ...OLD_CONFIG,
      statusManageRoles: [],
    });
    expect(parsed.statusManageRoles).toEqual(["manager"]);
    expect(normalizeStatusManageRoles(["editor"])).toEqual([
      "manager",
      "editor",
    ]);
    expect(normalizeStatusManageRoles(["editor", "editor", "manager"])).toEqual(
      ["manager", "editor"],
    );
  });
});

describe("resolveKanbanSettings", () => {
  it("gives today's behaviour for an empty or unknown-shaped config", () => {
    const defaults = {
      laneCardLimit: 4,
      statusManageRoles: ["manager", "editor"],
      keyboardMove: true,
    };
    expect(resolveKanbanSettings({})).toEqual(defaults);
    expect(
      resolveKanbanSettings({
        laneCardLimit: "x",
        statusManageRoles: "x",
        keyboardMove: "x",
      }),
    ).toEqual(defaults);
  });

  it("clamps the limit, locks manager in and honours the switch", () => {
    expect(resolveKanbanSettings({ laneCardLimit: 99 }).laneCardLimit).toBe(20);
    expect(resolveKanbanSettings({ laneCardLimit: -3 }).laneCardLimit).toBe(1);
    expect(resolveKanbanSettings({ laneCardLimit: 6.4 }).laneCardLimit).toBe(6);
    expect(
      resolveKanbanSettings({ statusManageRoles: ["editor"] })
        .statusManageRoles,
    ).toEqual(["manager", "editor"]);
    expect(
      resolveKanbanSettings({ statusManageRoles: [] }).statusManageRoles,
    ).toEqual(["manager"]);
    expect(resolveKanbanSettings({ keyboardMove: false }).keyboardMove).toBe(
      false,
    );
  });
});
