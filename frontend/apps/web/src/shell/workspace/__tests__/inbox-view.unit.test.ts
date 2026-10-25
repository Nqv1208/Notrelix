import { describe, expect, it } from "vitest";
import { workspaceInboxCopy } from "@notrelix/features-workspace/web";
import { snoozeUntil, toInboxEntries } from "../inbox-view";
import { toInboxDto } from "../work-feed-bridge";
import type { InboxEntryDto } from "../work-feed-bridge";

const NOW = Date.parse("2026-10-12T10:40:00+07:00");
const copy = workspaceInboxCopy("vi");
const deps = {
  copy,
  now: NOW,
  departments: new Map(),
  hrefFor: () => null,
  weekday: () => "thứ Tư",
};

const base: InboxEntryDto = {
  id: "n1",
  createdAt: "2026-10-12T03:28:00.000Z",
  unread: true,
  type: "approval_requested",
  actor: { id: "u1", name: "Trần Minh Quân" },
  need: "approve",
  approval: null,
  following: false,
  muted: false,
  done: false,
  snoozedUntil: null,
  target: {
    kind: "item",
    id: "i1",
    key: "ERP-138",
    title: "Thiết lập SSO",
    boardId: "b1",
    boardName: "Triển khai ERP Q4",
    departmentId: null,
    owner: null,
  },
  excerpt: null,
  dueAt: null,
  permission: null,
  bundle: null,
  detail: {
    approval: {
      soleApprover: true,
      summary: "cấu hình đăng nhập một lần",
      changes: [],
    },
    item: {
      status: { optionId: "s", label: "Chờ duyệt", bucket: "review" },
      priority: "high",
      dueAt: "2026-10-14T10:00:00.000Z",
      commentCount: 4,
    },
    latestComment: null,
  },
};

describe("toInboxEntries", () => {
  it("builds the approval row and its reading pane in Vietnamese", () => {
    const [item] = toInboxEntries([base], deps);
    expect(item?.segments.map((s) => s.text).join("")).toBe(
      "Trần Minh Quân gửi bạn duyệt Thiết lập SSO",
    );
    expect(item?.context?.label).toBe("Triển khai ERP Q4 · ERP-138");
    expect(item?.need).toBe("approve");
    expect(item?.detail?.event.when).toBe(
      "Hôm nay 10:28 · bạn là người duyệt duy nhất · cần duyệt trước thứ Tư 14/10",
    );
    expect(item?.detail?.card?.priority).toBe("Cao");
    expect(item?.detail?.card?.approvable).toBe(true);
  });

  it("counts the days an overdue item is late", () => {
    const [item] = toInboxEntries(
      [
        {
          ...base,
          type: "overdue",
          need: "overdue",
          actor: null,
          dueAt: "2026-10-10T10:00:00.000Z",
          detail: null,
        },
      ],
      deps,
    );
    expect(item?.segments.map((s) => s.text).join("")).toContain(
      "đã quá hạn 2 ngày",
    );
  });
});

describe("unknown and automation notifications", () => {
  it("draws a type the page does not know as a generic system row with its own text", () => {
    const dto = toInboxDto("w1", {
      items: [
        {
          id: "n9",
          type: "billing.something-new",
          createdAt: "2026-10-12T03:28:00.000Z",
          unread: true,
          excerpt: "Your plan changed",
        },
      ],
    } as never);
    expect(dto.items).toHaveLength(1);
    expect(dto.items[0]?.type).toBe("system");
    const [row] = toInboxEntries(dto.items, deps);
    expect(row?.kind).toBe("system");
    expect(row?.segments.map((s) => s.text).join("")).toBe("Your plan changed");
  });

  it("draws an automation message about an item", () => {
    const dto = toInboxDto("w1", {
      items: [
        {
          id: "n8",
          type: "work.item.automation",
          createdAt: "2026-10-12T03:28:00.000Z",
          unread: true,
          excerpt: "Please check",
          target: { kind: "item", id: "i1", title: "Thiết lập SSO" },
        },
      ],
    } as never);
    expect(dto.items[0]?.type).toBe("automation");
    const [row] = toInboxEntries(dto.items, deps);
    expect(row?.segments.map((s) => s.text).join("")).toContain(
      "Thiết lập SSO",
    );
  });

  it("marks a mention that needs a reply, a muted thread and a done entry", () => {
    const dto = toInboxDto(
      "w1",
      {
        items: [
          {
            id: "n7",
            type: "mention.created",
            createdAt: "2026-10-12T03:28:00.000Z",
            unread: false,
            needsReply: true,
            muted: true,
            excerpt: "ping",
          },
        ],
      } as never,
      "done",
    );
    const entry = dto.items[0];
    expect(entry?.need).toBe("reply");
    expect(entry?.muted).toBe(true);
    expect(entry?.done).toBe(true);
    expect(dto.counts.need).toBe(1);
  });
});

describe("approval notifications", () => {
  const requested = (status: string) =>
    toInboxDto("w1", {
      items: [
        {
          id: "n6",
          type: "approval.requested",
          createdAt: "2026-10-12T03:28:00.000Z",
          unread: true,
          actor: { id: "u1", name: "Quân" },
          excerpt: "Please look",
          target: { kind: "item", id: "i1", title: "Thiết lập SSO" },
          approval: { requestId: "r1", status },
        },
      ],
    } as never);

  it("lets the named approver decide while the request is pending", () => {
    const dto = requested("Pending");
    expect(dto.items[0]?.type).toBe("approval_requested");
    expect(dto.items[0]?.need).toBe("approve");
    expect(dto.items[0]?.approval).toEqual({
      requestId: "r1",
      status: "pending",
    });
    const [row] = toInboxEntries(dto.items, deps);
    expect(row?.detail?.card?.approvable).toBe(true);
  });

  it("offers no decision once the request was decided", () => {
    const dto = requested("Approved");
    expect(dto.items[0]?.need).toBeNull();
    const [row] = toInboxEntries(dto.items, deps);
    expect(row?.detail?.card?.approvable).toBe(false);
  });

  it("tells the requester about the decision with the approver's words", () => {
    const dto = toInboxDto("w1", {
      items: [
        {
          id: "n5",
          type: "approval.rejected",
          createdAt: "2026-10-12T03:28:00.000Z",
          unread: true,
          actor: { id: "u2", name: "Lan" },
          excerpt: "Needs a budget",
          target: { kind: "item", id: "i1", title: "Thiết lập SSO" },
        },
      ],
    } as never);
    expect(dto.items[0]?.type).toBe("approval_rejected");
    const [row] = toInboxEntries(dto.items, deps);
    expect(row?.segments.map((s) => s.text).join("")).toBe(
      "Lan yêu cầu sửa Thiết lập SSO: “Needs a budget”",
    );
  });
});

describe("snoozeUntil", () => {
  const monday = new Date(2026, 9, 12, 14, 0, 0).getTime();
  it("is an hour, the next morning or the next Monday morning", () => {
    expect(Date.parse(snoozeUntil("hour", monday)) - monday).toBe(3_600_000);
    const tomorrow = new Date(snoozeUntil("tomorrow", monday));
    expect([tomorrow.getDate(), tomorrow.getHours()]).toEqual([13, 8]);
    const week = new Date(snoozeUntil("next-week", monday));
    expect([week.getDate(), week.getDay(), week.getHours()]).toEqual([
      19, 1, 8,
    ]);
  });
});
