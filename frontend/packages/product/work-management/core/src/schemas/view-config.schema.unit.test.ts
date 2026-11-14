import { describe, expect, it } from "vitest";
import {
  DEFAULT_KANBAN_LANE_CARD_LIMIT,
  normalizeStatusManageRoles,
  resolveCalendarDateField,
  resolveTimelinePresentation,
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
      laneCardLimit: DEFAULT_KANBAN_LANE_CARD_LIMIT,
      statusManageRoles: ["manager", "editor"],
      keyboardMove: true,
      wipLimits: {},
      cardFieldIds: [],
      cardHideEmpty: [],
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

describe("Calendar and Timeline presentation keys", () => {
  it("gives the defaults for a config saved before the keys", () => {
    expect(resolveCalendarDateField({})).toBe("span");
    expect(resolveTimelinePresentation({})).toEqual({
      groupBy: "group",
      barSpan: "start-due",
    });
  });

  it("keeps a saved date field and a saved grouping, and drops bad values", () => {
    expect(resolveCalendarDateField({ calendarDateField: "f-1" })).toBe("f-1");
    expect(
      resolveTimelinePresentation({
        timelineGroupBy: "assignee",
        timelineBarSpan: "created-due",
      }),
    ).toEqual({ groupBy: "assignee", barSpan: "created-due" });
    expect(resolveCalendarDateField({ calendarDateField: 3 })).toBe("span");
    expect(resolveTimelinePresentation({ timelineGroupBy: "x" }).groupBy).toBe(
      "group",
    );
  });
});
