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

  it("words a first status without an empty 'from', in both languages", () => {
    const event = {
      code: "work.item-status-changed",
      change: { fieldName: "Status", from: "", to: "Cần làm" },
    };

    expect(describeActivityEvent(event, ACTIVITY_WORDS_EN)?.verb).toBe(
      "set the status to Cần làm",
    );
    expect(describeActivityEvent(event, ACTIVITY_WORDS_VI)?.verb).toBe(
      "đặt trạng thái là Cần làm",
    );
  });

  it("words label additions and removals from the label name", () => {
    const added = { code: "work.item-label-added", change: { to: "Bug" } };
    const removed = { code: "work.item-label-removed", change: { to: "Bug" } };

    expect(describeActivityEvent(added, ACTIVITY_WORDS_EN)?.verb).toBe(
      "added the label Bug",
    );
    expect(describeActivityEvent(removed, ACTIVITY_WORDS_EN)?.verb).toBe(
      "removed the label Bug",
    );
    expect(describeActivityEvent(added, ACTIVITY_WORDS_VI)?.verb).toBe(
      "thêm nhãn Bug",
    );
    expect(describeActivityEvent(removed, ACTIVITY_WORDS_VI)?.verb).toBe(
      "gỡ nhãn Bug",
    );
  });

  it("words checklist history from the checklist and to-do titles, in both languages", () => {
    const list = { to: "Release" };
    const todo = { fieldName: "Release", to: "Tag build" };
    const cases: ReadonlyArray<[string, object, string, string]> = [
      [
        "work.item-checklist-added",
        list,
        "added the checklist Release",
        "thêm danh sách kiểm tra Release",
      ],
      [
        "work.item-checklist-renamed",
        list,
        "renamed a checklist to Release",
        "đổi tên một danh sách kiểm tra thành Release",
      ],
      [
        "work.item-checklist-deleted",
        list,
        "deleted the checklist Release",
        "xóa danh sách kiểm tra Release",
      ],
      [
        "work.item-checklist-item-added",
        todo,
        "added Tag build to the checklist Release",
        "thêm Tag build vào danh sách kiểm tra Release",
      ],
      [
        "work.item-checklist-item-completed",
        todo,
        "completed Tag build in the checklist Release",
        "hoàn thành Tag build trong danh sách kiểm tra Release",
      ],
      [
        "work.item-checklist-item-reopened",
        todo,
        "marked Tag build as not done in the checklist Release",
        "bỏ hoàn thành Tag build trong danh sách kiểm tra Release",
      ],
      [
        "work.item-checklist-item-removed",
        todo,
        "removed Tag build from the checklist Release",
        "xóa Tag build khỏi danh sách kiểm tra Release",
      ],
    ];
    for (const [code, change, en, vi] of cases) {
      expect(
        describeActivityEvent({ code, change }, ACTIVITY_WORDS_EN)?.verb,
      ).toBe(en);
      expect(
        describeActivityEvent({ code, change }, ACTIVITY_WORDS_VI)?.verb,
      ).toBe(vi);
    }
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
