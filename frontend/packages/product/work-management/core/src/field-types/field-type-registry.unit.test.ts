import { describe, expect, it } from "vitest";
import type { FieldDefinition, FieldType } from "../types/field-types";
import {
  cellValueOf,
  compareFieldValues,
  fieldTypeRegistry,
  groupKeyOf,
  specOf,
  type FieldContext,
} from "./field-type-registry";

function field(
  fieldType: FieldType,
  options: FieldDefinition["options"] = [],
): FieldDefinition {
  return {
    id: "f1",
    boardId: "b1",
    name: "F",
    fieldType,
    options,
    position: 1,
    isHidden: false,
    isSystemField: false,
  };
}

const names: Record<string, string> = { u1: "An", u2: "Bình" };
const context = (
  fieldType: FieldType,
  options: FieldDefinition["options"] = [],
): FieldContext => ({
  field: field(fieldType, options),
  memberName: (id) => names[id] ?? id,
});

const sorted = (
  fieldType: FieldType,
  values: unknown[],
  direction: "asc" | "desc",
  options: FieldDefinition["options"] = [],
) =>
  [...values].sort((a, b) =>
    compareFieldValues(context(fieldType, options), a, b, direction),
  );

describe("fieldTypeRegistry", () => {
  it("has a spec for every type, keyed by itself", () => {
    for (const [type, spec] of Object.entries(fieldTypeRegistry)) {
      expect(spec.type).toBe(type);
      expect(spec.operators.length).toBeGreaterThan(0);
    }
  });

  it("marks only Formula and Rollup as computed and read-only", () => {
    const computed = Object.values(fieldTypeRegistry)
      .filter((spec) => spec.computed)
      .map((spec) => spec.type);
    expect(computed.sort()).toEqual(["formula", "rollup"]);
    expect(fieldTypeRegistry.formula.editable).toBe(false);
    expect(fieldTypeRegistry.formula.parseDraft("1")).toEqual({
      ok: false,
      error: "read_only",
    });
    expect(fieldTypeRegistry.number.editable).toBe(true);
  });
});

describe("null rule", () => {
  it("sorts empty values last ascending and first descending", () => {
    const values = [3, null, 1, "", 2];
    expect(sorted("number", values, "asc")).toEqual([1, 2, 3, null, ""]);
    expect(sorted("number", values, "desc")).toEqual([null, "", 3,
      2,
      1,
    ]);
  });

  it("treats an unticked checkbox as a value, not as empty", () => {
    expect(sorted("checkbox", [true, null, false], "asc")).toEqual([
      false,
      true,
      null,
    ]);
  });

  it("orders text without regard to case and diacritics", () => {
    expect(sorted("text", ["b", "A", "a1", ""], "asc")).toEqual([
      "A",
      "a1",
      "b",
      "",
    ]);
  });
});

describe("compare by type", () => {
  it("orders options by board order, not by name", () => {
    const options = [
      { id: "o1", label: "Zebra", color: "#000" },
      { id: "o2", label: "Alpha", color: "#000" },
    ];
    expect(sorted("select", ["o2", "o1"], "asc", options)).toEqual([
      "o1",
      "o2",
    ]);
  });

  it("orders dates and people", () => {
    expect(sorted("date", ["2026-10-20", "2026-10-02", null], "asc")).toEqual([
      "2026-10-02",
      "2026-10-20",
      null,
    ]);
    expect(sorted("person", [["u2"], ["u1"]], "asc")).toEqual([["u1"], ["u2"]]);
  });
});

describe("parseDraft", () => {
  it("parses numbers, accepting a decimal comma and clearing on empty", () => {
    const parse = fieldTypeRegistry.number.parseDraft;
    expect(parse("3,5")).toEqual({ ok: true, value: 3.5 });
    expect(parse("  ")).toEqual({ ok: true, value: null });
    expect(parse("abc")).toEqual({ ok: false, error: "invalid_number" });
  });

  it("accepts http(s) links, adds a scheme to a bare host and rejects others", () => {
    const parse = fieldTypeRegistry.link.parseDraft;
    expect(parse("erp.minhan.vn/t/1")).toEqual({
      ok: true,
      value: "https://erp.minhan.vn/t/1",
    });
    expect(parse("javascript:alert(1)")).toEqual({
      ok: false,
      error: "invalid_link",
    });
    expect(parse("")).toEqual({ ok: true, value: null });
  });

  it("accepts a calendar day only", () => {
    const parse = fieldTypeRegistry.date.parseDraft;
    expect(parse("2026-10-12")).toEqual({ ok: true, value: "2026-10-12" });
    expect(parse("12/10/2026")).toEqual({ ok: false, error: "invalid_date" });
  });
});

describe("text and group", () => {
  it("shows ids as words", () => {
    expect(
      specOf(field("multi_select")).text(["o1", "o2"], {
        ...context("multi_select", [
          { id: "o1", label: "Kế toán", color: "#000" },
          { id: "o2", label: "Kho", color: "#000" },
        ]),
      }),
    ).toBe("Kế toán, Kho");
    expect(specOf(field("person")).text(["u1", "u2"], context("person"))).toBe(
      "An, Bình",
    );
  });

  it("groups select, person, date and checkbox; empty goes to the empty bucket", () => {
    expect(groupKeyOf(field("select"), "o1")).toBe("o1");
    expect(groupKeyOf(field("select"), null)).toBe("");
    expect(groupKeyOf(field("text"), "x")).toBeNull();
  });
});

describe("cellValueOf", () => {
  it("reads the computed result for a Formula and the stored value otherwise", () => {
    const card = {
      fieldValues: { n: 5 },
      computedValues: {
        f: { state: "ok" as const, value: 10 },
        g: { state: "error" as const, errorCode: "divide-by-zero" },
      },
    };
    expect(cellValueOf({ id: "n", fieldType: "number" }, card)).toEqual({
      value: 5,
    });
    expect(cellValueOf({ id: "f", fieldType: "formula" }, card)).toEqual({
      value: 10,
      computed: { state: "ok", value: 10 },
    });
    expect(cellValueOf({ id: "g", fieldType: "formula" }, card).value).toBe(
      undefined,
    );
  });
});
