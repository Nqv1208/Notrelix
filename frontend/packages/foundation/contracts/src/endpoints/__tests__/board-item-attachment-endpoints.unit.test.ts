import { describe, expect, it } from "vitest";
import { endpoints } from "../endpoints";

describe("board item attachment routes", () => {
  it("addresses one attachment of an item under the list route", () => {
    expect(endpoints.boardItems.attachments("i1")).toBe(
      "/board-items/i1/attachments",
    );
    expect(endpoints.boardItems.attachment("i1", "a1")).toBe(
      "/board-items/i1/attachments/a1",
    );
    expect(endpoints.boardItems.attachmentContent("i1", "a1")).toBe(
      "/board-items/i1/attachments/a1/content",
    );
    expect(endpoints.boardItems.attachmentFiles("i1")).toBe(
      "/board-items/i1/attachments/files",
    );
  });
});
