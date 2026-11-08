import { describe, expect, it } from "vitest";
import { boardSettingsCopy } from "../../board-settings/board-settings.copy";
import { limitCopy } from "../../board-settings/board-settings-format";

describe("a limit refusal says the limit and its max (UC-WM-T03)", () => {
  it("names the max in English", () => {
    const copy = boardSettingsCopy("en");
    expect(limitCopy(copy, { name: "items", max: 500 })).toContain("500");
    expect(limitCopy(copy, { name: "bulkItems", max: 200 })).toContain("200");
  });

  it("names the max in Vietnamese", () => {
    const copy = boardSettingsCopy("vi");
    expect(limitCopy(copy, { name: "items", max: 500 })).toContain("500");
    expect(limitCopy(copy, { name: "bulkItems", max: 200 })).toContain("200");
  });

  it("falls back to the general wording for an unknown limit", () => {
    const copy = boardSettingsCopy("en");
    expect(limitCopy(copy, { name: "unknown", max: 7 })).toContain("7");
  });
});
