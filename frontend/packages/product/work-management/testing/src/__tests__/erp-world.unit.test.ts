import { describe, expect, it } from "vitest";
import { groupCardsByStatus } from "@notrelix/work-management-core";
import {
  commenterCapabilities,
  ERP_NOW,
  erpBoardScenario,
  erpClock,
  erpCommenterAccess,
  erpFullBoardDto,
  erpOwnerAccess,
} from "../index";

function cardsOf() {
  return erpBoardScenario().groups.flatMap((group) => group.cards);
}

function localDay(iso: string): string {
  // The ERP world is in Asia/Ho_Chi_Minh (+07:00, no daylight saving).
  const shifted = new Date(Date.parse(iso) + 7 * 3_600_000);
  return shifted.toISOString().slice(0, 10);
}

describe("ERP world fixtures", () => {
  it("is deterministic and pinned to Monday 12/10/2026 10:40 in Hanoi", () => {
    expect(erpFullBoardDto()).toEqual(erpFullBoardDto());
    expect(ERP_NOW).toBe("2026-10-12T10:40:00+07:00");
    expect(erpClock().toISOString()).toBe("2026-10-12T03:40:00.000Z");
  });

  it("has 16 top-level items in lanes Cần làm 6, Đang làm 3, Chờ duyệt 2, Hoàn thành 5", () => {
    const lanes = groupCardsByStatus(erpBoardScenario());
    expect(lanes.map((lane) => [lane.name, lane.category, lane.total])).toEqual(
      [
        ["Cần làm", "todo", 6],
        ["Đang làm", "in_progress", 3],
        ["Chờ duyệt", "review", 2],
        ["Hoàn thành", "done", 5],
      ],
    );
    expect(lanes.map((lane) => lane.cards.map((card) => card.itemKey))).toEqual(
      [
        ["ERP-151", "ERP-149", "ERP-147", "ERP-150", "ERP-152", "ERP-153"],
        ["ERP-142", "ERP-144", "ERP-146"],
        ["ERP-138", "ERP-140"],
        ["ERP-131", "ERP-129", "ERP-136", "ERP-133", "ERP-135"],
      ],
    );
    expect(new Set(cardsOf().map((card) => card.itemKey)).size).toBe(16);
  });

  it("matches the board's priority totals: Khẩn 1, Cao 5, Trung bình 7, Thấp 3", () => {
    const counts = cardsOf().reduce<Record<string, number>>((acc, card) => {
      const key = card.priority ?? "none";
      acc[key] = (acc[key] ?? 0) + 1;
      return acc;
    }, {});
    expect(counts).toEqual({ urgent: 1, high: 5, medium: 7, low: 3 });
  });

  it("carries ERP-142 as the mockup shows it", () => {
    const card = cardsOf().find((c) => c.itemKey === "ERP-142")!;
    expect(card).toMatchObject({
      title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      priority: "urgent",
      statusCategory: "in_progress",
      childProgress: { done: 3, total: 5 },
      labels: [expect.objectContaining({ name: "Dữ liệu" })],
    });
    expect(card.members.map((member) => member.name)).toEqual([
      "Trần Minh Quân",
    ]);
    expect(localDay(card.dueAt!)).toBe("2026-10-10");
    expect(localDay(card.startDate!)).toBe("2026-10-01");
  });

  it("gives every item a date except ERP-153, and only ERP-142 is overdue", () => {
    const cards = cardsOf();
    expect(
      cards.filter((card) => !card.dueAt).map((card) => card.itemKey),
    ).toEqual(["ERP-153"]);
    const now = erpClock().getTime();
    const overdue = cards.filter(
      (card) =>
        card.dueAt &&
        card.statusCategory !== "done" &&
        localDay(card.dueAt) < localDay(erpClock().toISOString()) &&
        Date.parse(card.dueAt) < now,
    );
    expect(overdue.map((card) => card.itemKey)).toEqual(["ERP-142"]);
    // The khong-ket-qua filter (Quá hạn + Ưu tiên Thấp) matches nothing.
    expect(overdue.filter((card) => card.priority === "low")).toEqual([]);
    const dueToday = cards.find((card) => card.itemKey === "ERP-144")!;
    expect(localDay(dueToday.dueAt!)).toBe("2026-10-12");
  });

  it("shows child progress only on ERP-151 0/4, ERP-142 3/5, ERP-144 6/9 and ERP-146 1/3", () => {
    const withChildren = cardsOf()
      .filter((card) => (card.childProgress?.total ?? 0) > 0)
      .map((card) => [card.itemKey, card.childProgress]);
    expect(withChildren).toEqual([
      ["ERP-151", { done: 0, total: 4 }],
      ["ERP-142", { done: 3, total: 5 }],
      ["ERP-144", { done: 6, total: 9 }],
      ["ERP-146", { done: 1, total: 3 }],
    ]);
  });

  it("lets Đỗ Khánh Linh comment but not edit, move or add", () => {
    expect(commenterCapabilities).toEqual({
      canCreateCard: false,
      canMoveCard: false,
      canRenameGroup: false,
      canDelete: false,
      canEditFields: false,
      canComment: true,
    });
    expect(erpCommenterAccess()).toMatchObject({
      myLevel: "commenter",
      mySource: {
        kind: "explicit",
        grantedBy: { name: "Lê Hoàng Phúc" },
      },
    });
  });

  it("gives Hà the manager level through the department and counts 32 people", () => {
    const access = erpOwnerAccess();
    expect(access).toMatchObject({
      myLevel: "manager",
      mySource: { kind: "workspace-admin", grantedBy: null },
      peopleCount: 32,
      guestCount: 1,
      peopleCountComplete: false,
    });
    expect(access.sources.reduce((sum, source) => sum + source.count, 0)).toBe(
      32,
    );
    // The commenter is not a manager: Teams and grantees stay unnamed.
    const commenter = erpCommenterAccess();
    expect(
      commenter.sources
        .filter((source) => source.kind === "team" || source.kind === "guest")
        .every((source) => source.name === null && source.id === null),
    ).toBe(true);
  });
});
