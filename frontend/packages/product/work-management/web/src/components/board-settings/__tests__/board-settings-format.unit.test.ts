import { describe, expect, it } from "vitest";
import {
  fieldLabel,
  fillCopy,
  plural,
  shortDay,
  systemFieldRows,
} from "../board-settings-format";
import { en, vi } from "../board-settings.copy";
import { erpBoardSettings } from "../../../verification/board-settings-fixtures";

describe("board settings format", () => {
  it("writes an instant as dd/MM in the board's time zone", () => {
    // 17:30 UTC on 1 October is already 2 October in Hanoi.
    expect(shortDay("2026-10-01T17:30:00.000Z", "Asia/Ho_Chi_Minh")).toBe(
      "02/10",
    );
    expect(shortDay("2026-10-01T17:30:00.000Z", "UTC")).toBe("01/10");
  });

  it("fills placeholders and leaves unknown ones", () => {
    expect(fillCopy("{{a}} và {{b}} {{c}}", { a: "x", b: 2 })).toBe(
      "x và 2 {{c}}",
    );
  });

  it("picks the one form for exactly 1", () => {
    expect(plural("{{count}} item", "{{count}} items", 1)).toBe("1 item");
    expect(plural("{{count}} item", "{{count}} items", 0)).toBe("0 items");
    expect(plural("{{count}} item", "{{count}} items", 16)).toBe("16 items");
  });

  it("lists a unit after the field name", () => {
    expect(fieldLabel({ name: "Ước lượng", unit: "giờ" })).toBe(
      "Ước lượng (giờ)",
    );
    expect(fieldLabel({ name: "Hạn chót", unit: null })).toBe("Hạn chót");
  });

  it("folds the locked system fields without a detail line into one row", () => {
    const rows = systemFieldRows(vi, erpBoardSettings().systemFields);
    expect(rows.map((row) => row.name)).toEqual([
      "Tên việc",
      "Mã việc",
      "Người tạo, Ngày tạo, Cập nhật lần cuối",
    ]);
    expect(rows[2]).toMatchObject({
      key: "created_by…",
      type: "Người, Ngày giờ",
      lock: "locked",
    });
    expect(rows[0]).toMatchObject({ lock: "rename", detail: "trường chính" });
    expect(systemFieldRows(en, erpBoardSettings().systemFields)[2]!.type).toBe(
      "Person, Date time",
    );
  });
});
