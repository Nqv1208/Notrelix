import { describe, expect, it } from "vitest";
import { mapBoardSettings, settingsWithRules } from "../api/board-settings.api";

describe("field rules in the settings JSON", () => {
  it("reads required, guidance and the default option off a field", () => {
    const board = {
      id: "b1",
      columns: [
        {
          id: "f1",
          name: "Ưu tiên",
          fieldType: "Select",
          settings: '{"required":true,"guidance":"Chọn một"}',
          defaultValue: '"o2"',
          options: [{ id: "o2", name: "Cao", color: "#fff", position: "a0" }],
        },
      ],
    };
    const [field] = mapBoardSettings("b1", board as never).fields;
    expect(field).toMatchObject({
      required: true,
      guidance: "Chọn một",
      defaultOptionId: "o2",
    });
  });

  it("merges an edit of the rules into the settings that are already there", () => {
    const merged = settingsWithRules('{"transitions":{},"required":true}', {
      required: false,
      guidance: " Điền đủ ",
    });
    expect(JSON.parse(merged!)).toEqual({
      transitions: {},
      guidance: " Điền đủ ",
    });
    expect(settingsWithRules("{}", { name: "x" })).toBeUndefined();
    expect(settingsWithRules("{}", { settings: '{"unit":"h"}' })).toBe(
      '{"unit":"h"}',
    );
  });
});

describe("the default of a text or number field", () => {
  it("reads the typed default and leaves a select's option id out of it", () => {
    const board = {
      id: "b1",
      columns: [
        { id: "t", name: "Ghi chú", fieldType: "Text", defaultValue: '"n/a"' },
        { id: "n", name: "Giờ", fieldType: "Number", defaultValue: "8" },
        { id: "s", name: "Loại", fieldType: "Select", defaultValue: '"o1"' },
      ],
    };
    const fields = mapBoardSettings("b1", board as never).fields;
    expect(fields.map((f) => f.defaultText)).toEqual(["n/a", "8", ""]);
  });
});

describe("the required refusal", () => {
  it("is told apart by its rule code", async () => {
    const { isRequiredRefusal } = await import("../queries/use-item-conflict");
    expect(
      isRequiredRefusal({ ruleCode: "WorkManagement_FieldValue_Required" }),
    ).toBe(true);
    expect(isRequiredRefusal({ ruleCode: "other" })).toBe(false);
    expect(isRequiredRefusal(null)).toBe(false);
  });
});

describe("a write over a published board limit (D78.8)", () => {
  it("reads the limit and its max off the 409, and nothing else is one", async () => {
    const { limitOf } = await import("../queries/use-board-settings");
    expect(
      limitOf({
        kind: "conflict",
        code: "work-management.limit.fields-per-board",
        details: { limit: "fields", current: 200, max: 200 },
      }),
    ).toEqual({ name: "fields", max: 200 });
    expect(
      limitOf({ kind: "conflict", code: "work-management.limit.new" }),
    ).toEqual({ name: "other", max: null });
    expect(limitOf({ kind: "conflict", code: "common.conflict" })).toBeNull();
    expect(limitOf(null)).toBeNull();
  });
});
