import { describe, expect, it } from "vitest";
import { workspaceInboxCopy } from "@notrelix/features-workspace/web";
import { toInboxEntries } from "../inbox-view";
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
  following: false,
  muted: false,
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
