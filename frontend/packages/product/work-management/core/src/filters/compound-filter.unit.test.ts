import { describe, expect, it } from "vitest";
import type { FieldDefinition, FieldType } from "../types/field-types";
import {
  parseSavedFilterCriteria,
  serializeSavedFilterCriteria,
} from "../types/saved-filter-types";
import type { FieldContext } from "../field-types/field-type-registry";
import {
  evaluateFilter,
  matchesCondition,
  parseCompoundFilter,
  resolveOperand,
  serializeCompoundFilter,
  type CompoundFilter,
  type ConditionOperator,
} from "./compound-filter";

const options = [
  { id: "o1", label: "Mới", color: "blue" },
  { id: "o2", label: "Xong", color: "green" },
] as unknown as FieldDefinition["options"];

const context = (fieldType: FieldType): FieldContext => ({
  field: {
    id: "f",
    boardId: "b",
    name: "F",
    fieldType,
    options,
    position: 1,
    isHidden: false,
    isSystemField: false,
  },
  memberName: (id) => ({ u1: "An", u2: "Bình" })[id] ?? id,
});

const hit = (
  type: FieldType,
  operator: ConditionOperator,
  values: string[],
  value: unknown,
) => matchesCondition(context(type), { operator, values }, value);

describe("matchesCondition, one test per operator", () => {
  it("contains ignores case and diacritics", () => {
    expect(hit("text", "contains", ["bao cao"], "Báo cáo Quý 3")).toBe(true);
    expect(hit("text", "contains", ["xyz"], "Báo cáo")).toBe(false);
    expect(hit("text", "contains", ["a"], null)).toBe(false);
  });
  it("not_contains is true for empty", () => {
    expect(hit("text", "not_contains", ["cao"], "Báo cáo")).toBe(false);
    expect(hit("text", "not_contains", ["cao"], "")).toBe(true);
  });
  it("equals by type", () => {
    expect(hit("number", "equals", ["5"], 5)).toBe(true);
    expect(hit("select", "equals", ["o1"], "o1")).toBe(true);
    expect(hit("select", "equals", ["o1"], "o2")).toBe(false);
    expect(hit("date", "equals", ["2026-10-12"], "2026-10-12T09:00:00Z")).toBe(
      true,
    );
    expect(hit("text", "equals", ["minh an"], "Minh An")).toBe(true);
  });
  it("not_equals", () => {
    expect(hit("number", "not_equals", ["5"], 6)).toBe(true);
    expect(hit("number", "not_equals", ["5"], 5)).toBe(false);
    expect(hit("number", "not_equals", ["5"], null)).toBe(true);
  });
  it("is_empty and is_not_empty use the type's empty rule", () => {
    expect(hit("person", "is_empty", [], [])).toBe(true);
    expect(hit("person", "is_empty", [], ["u1"])).toBe(false);
    expect(hit("text", "is_not_empty", [], "x")).toBe(true);
    expect(hit("number", "is_empty", [], 0)).toBe(false);
  });
  it("gt, gte, lt, lte", () => {
    expect(hit("number", "gt", ["5"], 6)).toBe(true);
    expect(hit("number", "gt", ["5"], 5)).toBe(false);
    expect(hit("number", "gte", ["5"], 5)).toBe(true);
    expect(hit("number", "lt", ["5"], 4)).toBe(true);
    expect(hit("number", "lt", ["5"], 5)).toBe(false);
    expect(hit("number", "lte", ["5"], 5)).toBe(true);
    expect(hit("number", "lte", ["5"], null)).toBe(false);
    expect(hit("number", "gt", ["abc"], 6)).toBe(false);
  });
  it("before and after compare days", () => {
    expect(hit("date", "before", ["2026-10-12"], "2026-10-11")).toBe(true);
    expect(hit("date", "before", ["2026-10-12"], "2026-10-12")).toBe(false);
    expect(hit("date", "after", ["2026-10-12"], "2026-10-13")).toBe(true);
    expect(hit("date", "after", ["2026-10-12"], null)).toBe(false);
  });
  it("between is an inclusive date range", () => {
    const range = ["2026-10-12", "2026-10-18"];
    expect(hit("date", "between", range, "2026-10-12")).toBe(true);
    expect(hit("date", "between", range, "2026-10-18")).toBe(true);
    expect(hit("date", "between", range, "2026-10-19")).toBe(false);
    expect(hit("date", "between", ["2026-10-12"], "2026-10-12")).toBe(false);
  });
  it("is_true and is_false: unset checkbox is false", () => {
    expect(hit("checkbox", "is_true", [], true)).toBe(true);
    expect(hit("checkbox", "is_true", [], false)).toBe(false);
    expect(hit("checkbox", "is_false", [], false)).toBe(true);
    expect(hit("checkbox", "is_false", [], null)).toBe(true);
  });
  it("has_any: a person field, Chưa giao is is_empty", () => {
    expect(hit("person", "has_any", ["u2", "u3"], ["u1", "u2"])).toBe(true);
    expect(hit("person", "has_any", ["u3"], ["u1"])).toBe(false);
    expect(hit("select", "has_any", ["o1", "o2"], "o2")).toBe(true);
    expect(hit("person", "has_any", ["u1"], null)).toBe(false);
    expect(hit("person", "has_any", ["__none__", "u1"], [])).toBe(true);
    expect(hit("person", "has_any", ["__none__"], ["u2"])).toBe(false);
  });
  it("has_all", () => {
    expect(hit("multi_select", "has_all", ["o1", "o2"], ["o1", "o2"])).toBe(
      true,
    );
    expect(hit("multi_select", "has_all", ["o1", "o2"], ["o1"])).toBe(false);
    expect(hit("multi_select", "has_all", [], ["o1"])).toBe(false);
  });
});

describe("evaluateFilter", () => {
  const fields: Record<string, { type: FieldType; value: unknown }> = {
    owner: { type: "person", value: [] },
    n: { type: "number", value: 7 },
  };
  const subject = {
    resolve: (id: string) =>
      fields[id]
        ? { context: context(fields[id].type), value: fields[id].value }
        : undefined,
  };
  const filter = (join: "and" | "or"): CompoundFilter => ({
    join,
    conditions: [
      { fieldId: "owner", operator: "is_empty", values: [] },
      { fieldId: "n", operator: "gt", values: ["10"] },
    ],
  });

  it("AND needs every condition, OR any", () => {
    expect(evaluateFilter(filter("and"), subject).matches).toBe(false);
    expect(evaluateFilter(filter("or"), subject).matches).toBe(true);
  });
  it("ignores a stale field and reports it", () => {
    const outcome = evaluateFilter(
      {
        join: "and",
        conditions: [{ fieldId: "gone", operator: "is_empty", values: [] }],
      },
      subject,
    );
    expect(outcome).toEqual({ matches: true, staleFieldIds: ["gone"] });
  });
  it("an empty filter matches everything", () => {
    expect(
      evaluateFilter({ join: "and", conditions: [] }, subject).matches,
    ).toBe(true);
  });
});

describe("URL form", () => {
  it("round-trips a compound filter with awkward ids and values", () => {
    const filter: CompoundFilter = {
      join: "or",
      conditions: [
        { fieldId: "a.b~c", operator: "contains", values: ["x.y~z, đ"] },
        { fieldId: "p", operator: "has_any", values: ["u1", "u2"] },
        {
          fieldId: "d",
          operator: "between",
          values: ["2026-10-12", "2026-10-18"],
        },
        { fieldId: "p", operator: "is_empty", values: [] },
      ],
    };
    expect(parseCompoundFilter(serializeCompoundFilter(filter))).toEqual(
      filter,
    );
  });
  it("keeps what it understands of a hand-edited string", () => {
    expect(
      parseCompoundFilter("c2:or~a.nope.x~b.equals.1~%E0.equals.1"),
    ).toEqual({
      join: "or",
      conditions: [{ fieldId: "b", operator: "equals", values: ["1"] }],
    });
  });
  it("leaves the legacy facet form to its own parser", () => {
    expect(parseCompoundFilter("p.low~o")).toBeUndefined();
    expect(serializeCompoundFilter({ join: "and", conditions: [] })).toBe("");
  });
});

describe("saved filter criteria version", () => {
  it("still parses version 1 criteria (no v) and a legacy filter string", () => {
    expect(
      parseSavedFilterCriteria('{"filter":"p.low~o","groupBy":"g"}'),
    ).toEqual({ filter: "p.low~o", groupBy: "g" });
  });
  it("writes version 2 and round-trips the swimlane field", () => {
    const json = serializeSavedFilterCriteria({
      filter: "c2:and~p.is_empty",
      swimlaneBy: "s",
    });
    expect(JSON.parse(json).v).toBe(2);
    expect(parseSavedFilterCriteria(json)).toEqual({
      filter: "c2:and~p.is_empty",
      swimlaneBy: "s",
    });
  });
});

describe("date operands in the workspace time zone", () => {
  const day = (operator: ConditionOperator, values: string[], value: string) =>
    matchesCondition(context("date"), { operator, values }, value, {
      timeZone: "Asia/Ho_Chi_Minh",
      now: new Date("2026-10-12T20:00:00Z"),
    });
  it("reads an instant near midnight as its day in UTC+7, not its UTC slice", () => {
    // 2026-10-12T20:00Z is 13/10 03:00 in Ho Chi Minh.
    expect(day("equals", ["2026-10-13"], "2026-10-12T20:00:00Z")).toBe(true);
    expect(day("equals", ["2026-10-12"], "2026-10-12T20:00:00Z")).toBe(false);
    expect(day("before", ["2026-10-13"], "2026-10-12T20:00:00Z")).toBe(false);
    expect(
      day("between", ["2026-10-13", "2026-10-13"], "2026-10-12T20:00:00Z"),
    ).toBe(true);
  });
  it("resolves relative operands on the clock of that zone", () => {
    expect(
      resolveOperand("@today", {
        now: new Date("2026-10-12T20:00:00Z"),
        timeZone: "Asia/Ho_Chi_Minh",
      }),
    ).toBe("2026-10-13");
    expect(day("equals", ["@today"], "2026-10-13T01:00:00Z")).toBe(true);
  });
  it("keeps a saved preset relative: it follows a later now", () => {
    const saved = parseCompoundFilter(
      serializeCompoundFilter({
        join: "and",
        conditions: [
          {
            fieldId: "f",
            operator: "between",
            values: ["@week-start", "@week-end"],
          },
        ],
      }),
    )!;
    const run = (now: string, value: string) =>
      evaluateFilter(
        saved,
        { resolve: () => ({ context: context("date"), value }) },
        { now: new Date(now) },
      ).matches;
    expect(run("2026-10-12T05:00:00Z", "2026-10-14")).toBe(true);
    expect(run("2026-10-21T05:00:00Z", "2026-10-14")).toBe(false);
    expect(run("2026-10-21T05:00:00Z", "2026-10-20")).toBe(true);
  });
  it("never throws on an oversized or malformed relative operand; it matches nothing", () => {
    const env = { now: new Date("2026-10-12T05:00:00Z") };
    for (const bad of [
      "@today+99999999999999",
      "@today-99999",
      "@bogus",
      "@today+",
    ]) {
      expect(() => resolveOperand(bad, env)).not.toThrow();
      expect(resolveOperand(bad, env)).toBe(bad);
      expect(
        evaluateFilter(
          {
            join: "and",
            conditions: [{ fieldId: "f", operator: "equals", values: [bad] }],
          },
          {
            resolve: () => ({ context: context("date"), value: "2026-10-12" }),
          },
          env,
        ).matches,
      ).toBe(false);
    }
    expect(resolveOperand("@today+9999", env)).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });
});
