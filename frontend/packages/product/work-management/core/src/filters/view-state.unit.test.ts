import { describe, expect, it } from "vitest";
import type { Card } from "../types/item-types";
import type { FieldDefinition } from "../types/field-types";
import {
  bucketKeyOf,
  buildFilterFields,
  cardMatchesFilter,
  dayOf,
  filterLanesByCompound,
  groupCardsByField,
  legacyFacetsToCompound,
  parseSorts,
  planLaneDrop,
  serializeSorts,
  sortCardsByLevels,
  staleFieldIdsOf,
  viewStateFromCriteria,
  viewStateToCriteria,
  type ViewState,
} from "./view-state";

const field = (
  id: string,
  fieldType: FieldDefinition["fieldType"],
  extra: Partial<FieldDefinition> = {},
): FieldDefinition => ({
  id,
  boardId: "b",
  name: id,
  fieldType,
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
  ...extra,
});

const priority = field("pri", "select", {
  semanticRole: "priority",
  name: "Ưu tiên",
  options: [
    { id: "p-high", label: "Cao", color: "" },
    { id: "p-low", label: "Thấp", color: "" },
  ],
});
const status = field("st", "select", {
  semanticRole: "status",
  name: "Trạng thái",
  options: [
    { id: "s-todo", label: "Cần làm", color: "" },
    { id: "s-doing", label: "Đang làm", color: "" },
  ],
});
const owner = field("own", "person", { name: "Người phụ trách" });
const due = field("due", "date", { name: "Hạn" });
const formula = field("fx", "formula", { name: "Rủi ro" });

const card = (id: string, extra: Partial<Card> = {}): Card =>
  ({
    id,
    title: id,
    members: [],
    labels: [],
    fieldValues: {},
    ...extra,
  }) as unknown as Card;

const names: Record<string, string> = { u1: "An", u2: "Bình" };
const memberName = (id: string) => names[id] ?? id;
const fields = buildFilterFields({
  fieldDefinitions: [status, priority, owner, due, formula],
  members: [
    { userId: "u1", name: "An" },
    { userId: "u2", name: "Bình" },
  ],
  labels: [{ id: "l1", name: "Lỗi" }],
  names: { title: "Tiêu đề", label: "Nhãn", overdue: "Quá hạn" },
  isOverdue: (c) => c.id === "late",
});

const cards = [
  card("a", {
    fieldValues: { own: ["u2"], due: "2026-10-14", pri: "p-low" },
    statusOptionId: "s-doing",
  }),
  card("b", {
    fieldValues: { own: ["u1"], due: "2026-10-12", pri: "p-high" },
    statusOptionId: "s-todo",
  }),
  card("c", { fieldValues: {}, statusOptionId: "s-todo" }),
  card("late", {
    fieldValues: { own: ["u1"], due: "2026-10-01", pri: "p-high" },
  }),
];

describe("filter fields", () => {
  it("offers the title, own fields, Nhãn and Quá hạn", () => {
    expect(fields.map((f) => f.id)).toEqual([
      "$title",
      "st",
      "pri",
      "own",
      "due",
      "fx",
      "$label",
      "$overdue",
    ]);
  });
  it("filters on a person, the pseudo overdue field and a stale field id", () => {
    const filter = {
      join: "and" as const,
      conditions: [
        { fieldId: "own", operator: "is_empty" as const, values: [] },
      ],
    };
    expect(
      cards
        .filter((c) => cardMatchesFilter(filter, fields, memberName, c))
        .map((c) => c.id),
    ).toEqual(["c"]);
    const overdue = {
      join: "and" as const,
      conditions: [
        { fieldId: "$overdue", operator: "is_true" as const, values: [] },
      ],
    };
    expect(
      cards
        .filter((c) => cardMatchesFilter(overdue, fields, memberName, c))
        .map((c) => c.id),
    ).toEqual(["late"]);
    const stale = {
      join: "and" as const,
      conditions: [
        { fieldId: "gone", operator: "equals" as const, values: ["x"] },
      ],
    };
    expect(staleFieldIdsOf(stale, fields)).toEqual(["gone"]);
    expect(
      cards.every((c) => cardMatchesFilter(stale, fields, memberName, c)),
    ).toBe(true);
  });
  it("keeps lane totals when it filters lanes", () => {
    const lanes = [
      {
        optionId: "s-todo",
        name: "x",
        category: null,
        color: null,
        cards: [cards[1]!, cards[2]!],
        total: 2,
      },
    ];
    const out = filterLanesByCompound(
      lanes,
      {
        join: "and",
        conditions: [{ fieldId: "own", operator: "has_any", values: ["u1"] }],
      },
      fields,
      memberName,
    );
    expect(out[0]!.cards.map((c) => c.id)).toEqual(["b"]);
    expect(out[0]!.total).toBe(2);
  });
});

describe("sortCardsByLevels", () => {
  it("sorts by date ascending with empty last, descending with empty first", () => {
    expect(
      sortCardsByLevels(
        cards,
        [{ fieldId: "due", direction: "asc" }],
        fields,
        memberName,
      ).map((c) => c.id),
    ).toEqual(["late", "b", "a", "c"]);
    expect(
      sortCardsByLevels(
        cards,
        [{ fieldId: "due", direction: "desc" }],
        fields,
        memberName,
      ).map((c) => c.id),
    ).toEqual(["c", "a", "b", "late"]);
  });
  it("breaks ties with the next level, then keeps input order", () => {
    const out = sortCardsByLevels(
      cards,
      [
        { fieldId: "pri", direction: "asc" },
        { fieldId: "due", direction: "desc" },
      ],
      fields,
      memberName,
    );
    expect(out.map((c) => c.id)).toEqual(["b", "late", "a", "c"]);
  });
  it("ignores a level whose field is gone", () => {
    expect(
      sortCardsByLevels(
        cards,
        [{ fieldId: "gone", direction: "asc" }],
        fields,
        memberName,
      ).map((c) => c.id),
    ).toEqual(["a", "b", "c", "late"]);
  });
});

describe("grouping", () => {
  const naming = { person: memberName, bucket: (key: string) => key };
  const lanes = (fieldId: string, bucket?: "week" | "month") =>
    groupCardsByField(
      cards,
      fields.find((f) => f.id === fieldId)!,
      bucket,
      naming,
    );

  it("groups by person by name with 'no value' last", () => {
    expect(
      lanes("own").map((l) => [l.optionId, l.name, l.cards.length]),
    ).toEqual([
      ["u1", "An", 2],
      ["u2", "Bình", 1],
      [null, "", 1],
    ]);
  });
  it("groups by week with Monday keys, chronological, none last", () => {
    expect(lanes("due", "week").map((l) => l.optionId)).toEqual([
      "2026-09-28",
      "2026-10-12",
      null,
    ]);
  });
  it("groups a select by option order including empty options", () => {
    expect(lanes("pri").map((l) => [l.optionId, l.cards.length])).toEqual([
      ["p-high", 2],
      ["p-low", 1],
      [null, 1],
    ]);
  });
  it("buckets days", () => {
    expect(bucketKeyOf("2026-10-12", "week")).toBe("2026-10-12");
    expect(bucketKeyOf("2026-10-18", "week")).toBe("2026-10-12");
    expect(bucketKeyOf("2026-10-18", "month")).toBe("2026-10");
    expect(bucketKeyOf("2026-11-02", "quarter")).toBe("2026-Q4");
    expect(bucketKeyOf("2026-11-02", "year")).toBe("2026");
    expect(dayOf("2026-10-12T17:00:00Z", "Asia/Ho_Chi_Minh")).toBe(
      "2026-10-13",
    );
  });
});

describe("planLaneDrop", () => {
  const person = fields.find((f) => f.id === "own")!;
  const base = {
    laneField: person,
    fromColumn: "s-doing",
    toColumn: "s-todo",
    fromLane: "u1",
    toLane: "u2",
  };
  it("changes both fields", () => {
    expect(planLaneDrop(base)).toMatchObject({
      ok: true,
      column: "s-todo",
      lane: ["u2"],
      laneChanged: true,
      columnChanged: true,
    });
  });
  it("changes only the lane, and clears it for the none lane", () => {
    expect(planLaneDrop({ ...base, toColumn: "s-doing" })).toMatchObject({
      ok: true,
      column: undefined,
      lane: ["u2"],
    });
    expect(
      planLaneDrop({ ...base, toColumn: "s-doing", toLane: null }),
    ).toMatchObject({ ok: true, lane: [] });
  });
  it("refuses a read-only lane field, a date lane and a full column", () => {
    expect(
      planLaneDrop({ ...base, laneField: fields.find((f) => f.id === "fx")! }),
    ).toEqual({ ok: false, reason: "lane_readonly" });
    expect(
      planLaneDrop({ ...base, laneField: fields.find((f) => f.id === "due")! }),
    ).toEqual({ ok: false, reason: "lane_derived" });
    expect(planLaneDrop({ ...base, wip: { limit: 2, count: 2 } })).toEqual({
      ok: false,
      reason: "wip",
    });
  });
  it("does not check WIP when the column stays", () => {
    expect(
      planLaneDrop({
        ...base,
        toColumn: "s-doing",
        wip: { limit: 1, count: 5 },
      }).ok,
    ).toBe(true);
  });
});

describe("legacy facets and criteria", () => {
  it("converts the legacy route filter to a compound filter on board fields", () => {
    const filter = legacyFacetsToCompound(
      {
        statusOptionIds: ["s-doing"],
        priorities: ["high"],
        assigneeIds: ["u1"],
        labelIds: ["l1"],
        overdue: true,
      },
      [status, priority, owner],
    );
    expect(
      filter.conditions.map((c) => [c.fieldId, c.operator, c.values]),
    ).toEqual([
      ["st", "has_any", ["s-doing"]],
      ["pri", "has_any", ["p-high"]],
      ["own", "has_any", ["u1"]],
      ["$label", "has_any", ["l1"]],
      ["$overdue", "is_true", []],
    ]);
  });
  it("round-trips the personal state through criteria, and a saved filter drops the sort", () => {
    const state: ViewState = {
      filter: {
        join: "or",
        conditions: [{ fieldId: "own", operator: "is_empty", values: [] }],
      },
      sorts: [
        { fieldId: "due", direction: "asc" },
        { fieldId: "pri", direction: "desc" },
      ],
      group: { fieldId: "due", bucket: "week" },
      lane: { fieldId: "own" },
    };
    expect(
      viewStateFromCriteria(viewStateToCriteria(state, { withSort: true })),
    ).toEqual(state);
    expect(
      viewStateToCriteria(state, { withSort: false }).sort,
    ).toBeUndefined();
  });
  it("parses sorts tolerantly and caps at 3", () => {
    expect(parseSorts("a.asc~a.desc~b.up~c.desc~d.asc~e.asc")).toEqual([
      { fieldId: "a", direction: "asc" },
      { fieldId: "c", direction: "desc" },
      { fieldId: "d", direction: "asc" },
    ]);
    expect(serializeSorts([{ fieldId: "x.y", direction: "asc" }])).toBe(
      "x%2Ey.asc",
    );
  });
});
