import { DndContext } from "@dnd-kit/core";
import { SortableContext } from "@dnd-kit/sortable";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import type { Card } from "@notrelix/work-management-core";
import {
  cardFixture,
  labelFixture,
  memberFixture,
} from "@notrelix/work-management-testing";
import { KanbanCard } from "../kanban-card";
import { kanbanCardCopy, type KanbanCardCopy } from "../kanban-card.copy";

/** 12/10/2026 10:40 in the runtime's own time zone. */
const NOW = new Date(2026, 9, 12, 10, 40);
const VI = kanbanCardCopy("vi");
const EN = kanbanCardCopy("en");

function dueOn(month: number, day: number, year = 2026): string {
  return new Date(year, month - 1, day, 17, 0).toISOString();
}

function members(count: number) {
  return Array.from({ length: count }, (_, index) =>
    memberFixture({
      id: `member-${index + 1}`,
      userId: `user-${index + 1}`,
      name: index === 0 ? "Trần Minh Quân" : `Member ${index + 1}`,
      initials: index === 0 ? "MQ" : `M${index + 1}`,
    }),
  );
}

function renderCard(
  overrides: Partial<Card> = {},
  props: {
    copy?: KanbanCardCopy;
    selected?: boolean;
    timeZone?: string;
    now?: Date;
    dragDisabled?: boolean;
  } = {},
) {
  const card = cardFixture({
    title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    ...overrides,
  });
  const onOpenDetails = vi.fn();
  const view = renderPureUi(
    <DndContext>
      <SortableContext items={[card.id]}>
        <KanbanCard
          card={card}
          now={props.now ?? NOW}
          timeZone={props.timeZone}
          copy={props.copy ?? VI}
          selected={props.selected}
          dragDisabled={props.dragDisabled}
          onOpenDetails={onOpenDetails}
        />
      </SortableContext>
    </DndContext>,
  );
  // The article is not named: the open control carries the title.
  const article = screen
    .getByRole("button", { name: card.title })
    .closest("article")!;
  return { ...view, card, article, onOpenDetails };
}

describe("KanbanCard interactions", () => {
  it("FUI[wm.kanban.card:card] opens by pointer, Enter and Space in vi and en, with no nested controls", () => {
    for (const copy of [VI, EN]) {
      const { card, article, onOpenDetails, unmount } = renderCard(
        {
          itemKey: "ERP-142",
          labels: [labelFixture({ name: "Dữ liệu" })],
          dueAt: dueOn(10, 10),
          priority: "urgent",
          childProgress: { done: 3, total: 5 },
          members: members(1),
        },
        { copy },
      );
      const open = within(article).getByRole("button", { name: card.title });
      expect(within(article).getAllByRole("button")).toHaveLength(1);

      fireEvent.click(open);
      fireEvent.keyDown(open, { key: "Enter", code: "Enter" });
      fireEvent.keyDown(open, { key: " ", code: "Space" });
      expect(onOpenDetails).toHaveBeenCalledTimes(3);
      expect(onOpenDetails).toHaveBeenLastCalledWith(card.id);
      // Enter and Space open the card; they never pick it up for a drag.
      expect(article.className).not.toContain("z-dropdown");

      const overdue = copy === VI ? "Quá hạn 2 ngày" : "Overdue 2 days";
      const urgent = copy === VI ? "Khẩn" : "Urgent";
      expect(within(article).getByText(overdue)).toBeTruthy();
      expect(within(article).getByText(urgent)).toBeTruthy();
      unmount();
    }
  });

  it("starts a pointer drag from anywhere on the card", () => {
    const { article } = renderCard();
    // jsdom has no PointerEvent: a mouse event typed pointerdown stands in.
    const pointerDown = new MouseEvent("pointerdown", {
      bubbles: true,
      button: 0,
    });
    Object.defineProperty(pointerDown, "isPrimary", { value: true });
    fireEvent(article, pointerDown);
    expect(article.className).toContain("z-dropdown");
    expect(article.getAttribute("style") ?? "").not.toContain("z-index");
  });

  it("never starts a drag when dragging is disabled (read-only board)", () => {
    const { article } = renderCard({}, { dragDisabled: true });
    const pointerDown = new MouseEvent("pointerdown", {
      bubbles: true,
      button: 0,
    });
    Object.defineProperty(pointerDown, "isPrimary", { value: true });
    fireEvent(article, pointerDown);
    expect(article.className).not.toContain("z-dropdown");
  });

  it("has no grip, menu or status badge, and shows the comment and file counts", () => {
    const { article } = renderCard({
      status: "status-working",
      linkedPageId: "page-1",
      _count: { comments: 3, attachments: 2, checklistItems: 4 },
    });
    expect(
      within(article).queryByRole("button", { name: /Di chuyển|Move/ }),
    ).toBeNull();
    expect(within(article).queryByRole("button", { name: /menu/i })).toBeNull();
    expect(article.querySelector("[data-slot='badge']")).toBeNull();
    // The face counts comments (3) and files (2); linked documents stay off
    // the card, and the checklist chip needs the item's checklist progress.
    expect(article.querySelector("[data-comment-count='3']")).not.toBeNull();
    expect(article.querySelector("[data-file-count='2']")).not.toBeNull();
    expect(article.querySelector("[data-checklist-progress]")).toBeNull();
    expect(article.textContent).not.toMatch(/tài liệu/i);
  });
});

describe("KanbanCard counts", () => {
  it("caps the comment and file counts at 99+ and keeps the real count readable", () => {
    const { article } = renderCard({
      _count: { comments: 100, attachments: 99, checklistItems: 0 },
    });
    const comments = article.querySelector("[data-comment-count]")!;
    const files = article.querySelector("[data-file-count]")!;
    expect(comments.textContent).toContain("99+");
    expect(files.textContent).toContain("99");
    expect(files.textContent).not.toContain("99+");
  });
});

describe("KanbanCard top row", () => {
  it("shows the item key in the mono font", () => {
    const { article } = renderCard({ itemKey: "ERP-142" });
    const key = within(article).getByText("ERP-142");
    expect(key.className).toContain("font-mono");
  });

  it("shows a plain-number key from a board without a prefix as #number", () => {
    const { article } = renderCard({ itemKey: "42" });
    expect(within(article).getByText("#42").className).toContain("font-mono");
  });

  it("renders no key and no label chip when both are missing", () => {
    const { article } = renderCard({ itemKey: null, labels: [] });
    expect(article.querySelector("[data-label-tone]")).toBeNull();
    expect(article.querySelector(".font-mono")).toBeNull();
  });

  it.each([
    ["Dữ liệu", "data"],
    ["Bảo mật", "security"],
    ["Đào tạo", "training"],
    ["Báo cáo", "report"],
    ["Tài liệu", "doc"],
    ["Hạ tầng", "doc"],
    ["Kiểm thử", "test"],
    ["SECURITY", "security"],
    ["Data", "data"],
  ])("maps the label %s to the %s tone token by name", (name, tone) => {
    const { article } = renderCard({
      labels: [labelFixture({ name, color: "#c4c4c4" })],
    });
    const chip = within(article).getByText(name);
    expect(chip.getAttribute("data-label-tone")).toBe(tone);
    expect(chip.className).toContain(`bg-label-${tone}`);
    expect(chip.className).toContain(`text-label-${tone}-foreground`);
  });

  it.each([
    ["#579bfc", "data"],
    ["#e2445c", "security"],
    ["#00c875", "green"],
    ["#fdab3d", "orange"],
    ["#a25ddc", "test"],
    ["#c4c4c4", "doc"],
    ["not-a-colour", "doc"],
  ])(
    "maps an unknown label name by its colour %s to the %s tone",
    (color, tone) => {
      const { article } = renderCard({
        labels: [labelFixture({ name: "Khách hàng", color })],
      });
      expect(
        within(article).getByText("Khách hàng").getAttribute("data-label-tone"),
      ).toBe(tone);
    },
  );

  it("shows up to two labels then a +N chip, and never paints the raw label colour", () => {
    const { article, container } = renderCard({
      labels: [
        labelFixture({ id: "l-1", name: "Dữ liệu", color: "#ff0000" }),
        labelFixture({ id: "l-2", name: "Bảo mật" }),
        labelFixture({ id: "l-3", name: "Kiểm thử" }),
      ],
    });
    expect(within(article).getByText("Dữ liệu")).toBeTruthy();
    expect(within(article).getByText("Bảo mật")).toBeTruthy();
    expect(within(article).queryByText("Kiểm thử")).toBeNull();
    expect(within(article).getByText("+1")).toBeTruthy();
    expect(container.innerHTML).not.toContain("#ff0000");
  });
});

describe("KanbanCard title and states", () => {
  it("names only the open control by the title, clamped to three lines", () => {
    const title =
      "Một tiêu đề rất dài để kiểm tra việc xuống dòng và cắt bớt sau ba dòng mà tên truy cập vẫn đầy đủ "
        .repeat(3)
        .trim();
    const { article } = renderCard({ title });
    const heading = within(article).getByRole("heading", { level: 3 });
    expect(heading.textContent).toBe(title);
    expect(heading.className).toContain("line-clamp-3");
    expect(within(article).getByRole("button", { name: title })).toBeTruthy();
    expect(article.hasAttribute("aria-label")).toBe(false);
  });

  it("describes the open control with the key, label, progress, due, priority and assignee", () => {
    const { article } = renderCard({
      itemKey: "ERP-142",
      labels: [labelFixture({ name: "Dữ liệu" })],
      childProgress: { done: 3, total: 5 },
      dueAt: dueOn(10, 10),
      priority: "urgent",
      members: members(1),
    });
    const open = within(article).getByRole("button");
    const parts = (open.getAttribute("aria-describedby") ?? "")
      .split(" ")
      .map((id) => document.getElementById(id));
    // Text content, plus the accessible name of the assignee group.
    const description = parts
      .map(
        (part) =>
          `${part?.textContent ?? ""} ${
            part?.querySelector("[role='group']")?.getAttribute("aria-label") ??
            ""
          }`,
      )
      .join(" ");
    expect(description).toContain("ERP-142");
    expect(description).toContain("Dữ liệu");
    expect(description).toContain("Đã xong 3 trên 5 việc con");
    expect(description).toContain("Quá hạn 2 ngày");
    expect(description).toContain("Khẩn");
    expect(description).toContain("Được giao cho Trần Minh Quân");
  });

  it("marks the selected card with aria-current and the selection ring", () => {
    const { article } = renderCard({}, { selected: true });
    expect(article.getAttribute("aria-current")).toBe("true");
    expect(article.className).toContain("ring-2");
  });

  it("does not mark an unselected card as current", () => {
    const { article } = renderCard();
    expect(article.getAttribute("aria-current")).toBeNull();
    expect(article.className).not.toContain("ring-2");
  });

  it("mutes the title of a done card and hides its due date", () => {
    const { article } = renderCard({
      statusCategory: "done",
      dueAt: dueOn(10, 2),
    });
    const heading = within(article).getByRole("heading", { level: 3 });
    expect(heading.className).toContain("text-muted-foreground");
    expect(article.querySelector("[data-due]")).toBeNull();
    expect(article.textContent).not.toContain("Quá hạn");
  });

  it("treats a card with a completion date as done", () => {
    const { article } = renderCard({
      completedAt: dueOn(10, 3),
      dueAt: dueOn(10, 2),
    });
    expect(article.querySelector("[data-due]")).toBeNull();
  });
});

describe("KanbanCard child progress", () => {
  it("shows the bar and done/total when the item has child items", () => {
    const { article } = renderCard({ childProgress: { done: 3, total: 5 } });
    const progress = article.querySelector("[data-child-progress]");
    expect(progress).not.toBeNull();
    expect(progress!.textContent).toContain("3/5");
    const fill = progress!.querySelector("[data-progress-fill]");
    expect((fill as HTMLElement).style.width).toBe("60%");
  });

  it("keeps a visible sliver for 0 of 4", () => {
    const { article } = renderCard({ childProgress: { done: 0, total: 4 } });
    const fill = article.querySelector("[data-progress-fill]") as HTMLElement;
    expect(fill.style.width).toBe("4%");
    expect(article.textContent).toContain("0/4");
  });

  it.each([
    ["0/0", { done: 0, total: 0 }],
    ["missing", null],
  ])("renders no progress for %s", (_case, childProgress) => {
    const { article } = renderCard({ childProgress });
    expect(article.querySelector("[data-child-progress]")).toBeNull();
  });

  it("never counts checklists as progress", () => {
    const { article } = renderCard({
      childProgress: null,
      _count: { comments: 0, attachments: 0, checklistItems: 3 },
    });
    expect(article.querySelector("[data-child-progress]")).toBeNull();
  });
});

describe("KanbanCard due wording", () => {
  it("writes a future due date as day/month", () => {
    const { article } = renderCard({ dueAt: dueOn(10, 18) });
    const due = article.querySelector("[data-due]")!;
    expect(due.getAttribute("data-due")).toBe("date");
    expect(due.textContent).toContain("18/10");
    expect(due.textContent).not.toMatch(/thg|Oct/);
    expect(due.className).not.toMatch(/danger|warning/);
  });

  it("pads a single-digit day and month", () => {
    const { article } = renderCard({ dueAt: dueOn(11, 5) });
    expect(article.querySelector("[data-due]")!.textContent).toContain("05/11");
  });

  it("adds the year for a date in another year", () => {
    const { article } = renderCard({ dueAt: dueOn(1, 18, 2027) });
    expect(article.querySelector("[data-due]")!.textContent).toContain(
      "18/01/2027",
    );
  });

  it("says Hôm nay on the warning role for a date due today", () => {
    const { article } = renderCard({ dueAt: dueOn(10, 12) });
    const due = article.querySelector("[data-due]")!;
    expect(due.getAttribute("data-due")).toBe("today");
    expect(due.textContent).toContain("Hôm nay");
    expect(due.className).toContain("warning-subtle");
  });

  it("keeps Hôm nay when the due time today has already passed", () => {
    const { article } = renderCard({
      dueAt: new Date(2026, 9, 12, 8, 0).toISOString(),
    });
    expect(article.querySelector("[data-due]")!.getAttribute("data-due")).toBe(
      "today",
    );
  });

  it.each([
    ["11/10", 10, 11, "Quá hạn 1 ngày", "Overdue 1 day"],
    ["10/10", 10, 10, "Quá hạn 2 ngày", "Overdue 2 days"],
    ["30/09", 9, 30, "Quá hạn 12 ngày", "Overdue 12 days"],
  ])(
    "counts the overdue days for %s in vi and en",
    (_date, month, day, viText, enText) => {
      const first = renderCard({ dueAt: dueOn(month, day) });
      const due = first.article.querySelector("[data-due]")!;
      expect(due.getAttribute("data-due")).toBe("overdue");
      expect(due.textContent).toBe(viText);
      expect(due.className).toContain("danger-subtle");
      first.unmount();
      const second = renderCard({ dueAt: dueOn(month, day) }, { copy: EN });
      expect(second.article.querySelector("[data-due]")!.textContent).toBe(
        enText,
      );
    },
  );

  it("judges the due date against the injected now", () => {
    const { article } = renderCard({ dueAt: dueOn(10, 13) });
    expect(article.querySelector("[data-due]")!.getAttribute("data-due")).toBe(
      "date",
    );
  });

  it("judges near-midnight due times in the given time zone", () => {
    // 00:10 on 12/10 in Hồ Chí Minh is still 11/10 in UTC.
    const now = new Date("2026-10-12T00:10:00+07:00");
    const dueAt = "2026-10-12T23:30:00+07:00";
    const local = renderCard({ dueAt }, { now, timeZone: "Asia/Ho_Chi_Minh" });
    expect(
      local.article.querySelector("[data-due]")!.getAttribute("data-due"),
    ).toBe("today");
    local.unmount();
    const utc = renderCard({ dueAt }, { now, timeZone: "UTC" });
    expect(
      utc.article.querySelector("[data-due]")!.getAttribute("data-due"),
    ).toBe("date");
  });

  it("falls back to the legacy due date", () => {
    const { article } = renderCard({ dueAt: null, dueDate: dueOn(10, 18) });
    expect(article.querySelector("[data-due]")!.textContent).toContain("18/10");
  });

  it.each([
    ["no due date", { dueAt: null, dueDate: undefined }],
    ["an unparseable due date", { dueAt: "not-a-date" }],
  ])("shows nothing for %s", (_case, overrides) => {
    const { article } = renderCard(overrides);
    expect(article.querySelector("[data-due]")).toBeNull();
  });
});

describe("KanbanCard priority", () => {
  it.each([
    ["urgent", "Khẩn", "text-danger-text"],
    ["high", "Cao", "text-priority-high-text"],
  ] as const)(
    "shows %s as an icon and the word %s",
    (priority, word, className) => {
      const { article } = renderCard({ priority });
      const marker = article.querySelector("[data-priority]")!;
      expect(marker.getAttribute("data-priority")).toBe(priority);
      expect(marker.textContent).toContain(word);
      expect(marker.querySelector("svg")?.getAttribute("aria-hidden")).toBe(
        "true",
      );
      expect(marker.className).toContain(className);
    },
  );

  it.each([
    ["medium", "Trung bình"],
    ["low", "Thấp"],
  ] as const)(
    "shows %s with its own glyph and the word %s",
    (priority, word) => {
      const { article } = renderCard({ priority });
      const marker = article.querySelector("[data-priority]")!;
      expect(marker.textContent).toContain(word);
      expect(marker.querySelector("svg")).not.toBeNull();
    },
  );

  it("gives each level a different glyph, so colour is never the only signal", () => {
    const glyphs = (["urgent", "high", "medium", "low"] as const).map(
      (priority) => {
        const { article, unmount } = renderCard({ priority });
        const html = article.querySelector("[data-priority] svg")!.innerHTML;
        unmount();
        return html;
      },
    );
    expect(new Set(glyphs).size).toBe(4);
  });

  it("shows nothing without a priority", () => {
    const { article } = renderCard({ priority: undefined });
    expect(article.querySelector("[data-priority]")).toBeNull();
  });

  it("localises the priority word", () => {
    const { article } = renderCard({ priority: "high" }, { copy: EN });
    expect(article.querySelector("[data-priority]")!.textContent).toContain(
      "High",
    );
  });
});

describe("KanbanCard assignee", () => {
  it("shows the avatar only, with the name in the accessible name", () => {
    const { article } = renderCard({ members: members(1) });
    const group = within(article).getByRole("group", {
      name: "Được giao cho Trần Minh Quân",
    });
    expect(group.textContent).toBe("MQ");
    expect(within(article).queryByText("Trần Minh Quân")).toBeNull();
  });

  it("names the other assignees without listing them", () => {
    const { article } = renderCard({ members: members(4) }, { copy: EN });
    const group = within(article).getByRole("group", {
      name: "Assigned to Trần Minh Quân and 3 more",
    });
    // Three avatars then a +N chip (4 people: MQ, M2, M3, +1).
    expect(group.textContent).toBe("MQM2M3+1");
  });

  it("shows no assignee for a card without members", () => {
    const { article } = renderCard({ members: [] });
    expect(within(article).queryByRole("group")).toBeNull();
  });

  it("paints the avatar with an identity tone, not the raw member colour", () => {
    const { container } = renderCard({
      members: [memberFixture({ color: "#ff0000" })],
    });
    expect(container.innerHTML).not.toContain("#ff0000");
    expect(container.innerHTML).toContain("bg-identity-");
  });
});

describe("KanbanCard markup", () => {
  it("uses no raw colours, arbitrary text sizes or raw z-index", () => {
    const { container } = renderCard({
      itemKey: "ERP-142",
      labels: [labelFixture({ name: "Dữ liệu", color: "#579bfc" })],
      childProgress: { done: 3, total: 5 },
      dueAt: dueOn(10, 10),
      priority: "urgent",
      members: members(2),
    });
    const html = container.innerHTML;
    expect(html).not.toMatch(/#[0-9a-fA-F]{3,8}\b/);
    expect(html).not.toMatch(/rgba?\(/);
    expect(html).not.toMatch(/text-\[\d/);
    expect(html).not.toMatch(/z-index/);
  });
});

describe("KanbanCard row 2 (checklist, files, blocked, crowding)", () => {
  const FULL = {
    dueAt: dueOn(16, 10),
    startDate: dueOn(12, 10),
    checklistProgress: { done: 3, total: 8 },
    _count: { comments: 12, attachments: 4, checklistItems: 8 },
    openBlockerCount: 2,
    members: members(3),
  };

  it.each([
    [VI, "Checklist đã xong 3 trên 8 mục", "Đang bị chặn bởi 2 việc", "4 tệp"],
    [EN, "Checklist 3 of 8 items done", "Blocked by 2 items", "4 files"],
  ] as const)("names every badge in %s", (copy, checklist, blocked, files) => {
    const { article } = renderCard(FULL, { copy });
    expect(within(article).getByText(checklist)).toBeTruthy();
    expect(within(article).getByText(blocked)).toBeTruthy();
    expect(within(article).getByText(files)).toBeTruthy();
  });

  it("is crowded from four chips: due date only, icon-only blocked, one avatar and +N", () => {
    const { article } = renderCard(FULL);
    const row = article.querySelector("[data-crowded]")!;
    expect(row).not.toBeNull();
    expect(row.className).toContain("flex-nowrap");
    expect(row.textContent).not.toContain("→");
    expect(row.querySelector("[data-blocked]")!.textContent).not.toContain(
      "Bị chặn",
    );
    expect(row.textContent).toContain("+2");
  });

  it("shows the start-to-due range with three chips or fewer", () => {
    const { article } = renderCard({
      dueAt: dueOn(16, 10),
      startDate: dueOn(12, 10),
      _count: { comments: 2, attachments: 0, checklistItems: 0 },
      checklistProgress: { done: 1, total: 2 },
    });
    expect(article.querySelector("[data-crowded]")).toBeNull();
    expect(article.querySelector("[data-due]")!.textContent).toContain("→");
  });

  it("keeps the word Bị chặn when the row has room", () => {
    const { article } = renderCard({ openBlockerCount: 1 });
    expect(article.querySelector("[data-blocked]")!.textContent).toContain(
      "Bị chặn",
    );
  });

  it("shows 99+ above 99 comments and no blocked badge at 0", () => {
    const { article } = renderCard({
      _count: { comments: 140, attachments: 0, checklistItems: 0 },
      openBlockerCount: 0,
    });
    expect(
      article.querySelector("[data-comment-count]")!.textContent,
    ).toContain("99+");
    expect(article.querySelector("[data-blocked]")).toBeNull();
  });
});
