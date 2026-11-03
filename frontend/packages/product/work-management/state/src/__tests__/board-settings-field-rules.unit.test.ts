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
