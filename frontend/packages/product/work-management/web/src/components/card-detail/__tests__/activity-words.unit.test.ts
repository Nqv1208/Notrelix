import { describe, expect, it } from "vitest";
import {
  ACTIVITY_WORDS_EN,
  ACTIVITY_WORDS_VI,
  describeActivityEvent,
} from "../activity-words";

describe("describeActivityEvent", () => {
  it("words a status change from the names, in Vietnamese and English", () => {
    const event = {
      code: "work.item-status-changed",
      change: { fieldName: "Status", from: "Cần làm", to: "Đang làm" },
    };

    expect(describeActivityEvent(event, ACTIVITY_WORDS_VI)).toEqual({
      verb: "đổi trạng thái từ Cần làm sang Đang làm",
    });
    expect(describeActivityEvent(event, ACTIVITY_WORDS_EN)).toEqual({
      verb: "changed the status from Cần làm to Đang làm",
    });
  });

  it("falls back to shorter words when a status name is unknown", () => {
    expect(
      describeActivityEvent(
        { code: "work.item-status-changed", change: { to: "Done" } },
        ACTIVITY_WORDS_EN,
      )?.verb,
    ).toBe("set the status to Done");
    expect(
      describeActivityEvent(
        { code: "work.item-status-changed" },
        ACTIVITY_WORDS_EN,
      )?.verb,
    ).toBe("changed the status");
  });

  it("words a field change with a before and after underneath, dates as dd/MM/yyyy", () => {
    expect(
      describeActivityEvent(
        {
          code: "work.item-field-changed",
          change: {
            fieldName: "Due date",
            from: "2026-10-08",
            to: "2026-10-10T00:00:00Z",
          },
        },
        ACTIVITY_WORDS_EN,
      ),
    ).toEqual({
      verb: "changed Due date",
      detail: { kind: "diff", from: "08/10/2026", to: "10/10/2026" },
    });
  });

  it("never shows a value for a field whose values are hidden", () => {
    const described = describeActivityEvent(
      { code: "work.item-field-changed", change: { fieldName: "Contract" } },
      ACTIVITY_WORDS_VI,
    );

    expect(described).toEqual({ verb: "đổi Contract" });
  });

  it("words people, moves and the lifecycle", () => {
    const en = ACTIVITY_WORDS_EN;
    expect(
      describeActivityEvent(
        { code: "work.item-assigned", change: { personName: "Binh" } },
        en,
      )?.verb,
    ).toBe("assigned Binh");
    expect(
      describeActivityEvent({ code: "work.item-assigned" }, en)?.verb,
    ).toBe("assigned someone");
    expect(
      describeActivityEvent(
        { code: "work.item-moved", change: { from: "Backlog", to: "Sprint" } },
        en,
      )?.verb,
    ).toBe("moved this item from Backlog to Sprint");
    expect(
      describeActivityEvent(
        {
          code: "work.item-moved-to-board",
          change: { from: "Website", to: "Mobile", itemKey: "MOB-7" },
        },
        ACTIVITY_WORDS_VI,
      )?.verb,
    ).toBe("chuyển việc từ bảng Website sang bảng Mobile");
    for (const code of [
      "work.item-created",
      "work.item-archived",
      "work.item-unarchived",
      "work.item-deleted",
      "work.item-restored",
    ]) {
      expect(describeActivityEvent({ code }, en)?.verb).toBeTruthy();
    }
  });

  it("shows a rename as a before and after", () => {
    expect(
      describeActivityEvent(
        { code: "work.item-renamed", change: { from: "Old", to: "New" } },
        ACTIVITY_WORDS_EN,
      ),
    ).toEqual({
      verb: "renamed this item",
      detail: { kind: "diff", from: "Old", to: "New" },
    });
  });

  it("leaves a line without a known type to its own words", () => {
    expect(describeActivityEvent({}, ACTIVITY_WORDS_EN)).toBeNull();
    expect(
      describeActivityEvent({ code: "work.something-new" }, ACTIVITY_WORDS_EN),
    ).toBeNull();
  });

  it("has the same words in both languages", () => {
    expect(Object.keys(ACTIVITY_WORDS_VI).sort()).toEqual(
      Object.keys(ACTIVITY_WORDS_EN).sort(),
    );
  });
});
