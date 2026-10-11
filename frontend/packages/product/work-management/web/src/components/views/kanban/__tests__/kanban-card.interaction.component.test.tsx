import { DndContext } from "@dnd-kit/core";
import { SortableContext } from "@dnd-kit/sortable";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import type { Card } from "@notrelix/work-management-core";
import {
  cardFixture,
  checklistFixture,
  checklistItemFixture,
  fixedClock,
  fixedIso,
  kanbanDefaultScenario,
  memberFixture,
} from "@notrelix/work-management-testing";
import { KanbanCard } from "../kanban-card";
import { kanbanCardCopy } from "../kanban-card.copy";

const NOW = fixedClock();

function members(count: number) {
  return Array.from({ length: count }, (_, index) =>
    memberFixture({
      id: `member-${index + 1}`,
      userId: `user-${index + 1}`,
      name: index === 0 ? "Avery Stone" : `Member ${index + 1}`,
      initials: index === 0 ? "AS" : `M${index + 1}`,
    }),
  );
}

function renderCard(
  overrides: Partial<Card> = {},
  props: Partial<Parameters<typeof KanbanCard>[0]> = {},
) {
  const scenario = kanbanDefaultScenario();
  const card = cardFixture({ title: "Ship the release notes", ...overrides });
  const handlers = {
    onOpenDetails: vi.fn(),
    onCopyLink: vi.fn(),
    onDuplicate: vi.fn(),
    onDelete: vi.fn(),
  };
  const view = renderPureUi(
    <DndContext>
      <SortableContext items={[card.id]}>
        <KanbanCard
          board={scenario.board}
          card={card}
          now={NOW}
          {...handlers}
          {...props}
        />
      </SortableContext>
    </DndContext>,
  );
  return { ...view, card, ...handlers };
}

const twoItemChecklist = checklistFixture({
  items: [
    checklistItemFixture({ id: "i-1", isDone: true }),
    checklistItemFixture({ id: "i-2", isDone: false }),
  ],
});

describe("KanbanCard interactions", () => {
  it("FUI[wm.kanban.card:card] opens by pointer and keyboard without bubbling nested actions", () => {
    const scenario = kanbanDefaultScenario();
    const card = scenario.columns[0]!.cards[0]!;
    const onOpenDetails = vi.fn();
    renderPureUi(
      <DndContext>
        <SortableContext items={[card.id]}>
          <KanbanCard
            board={scenario.board}
            card={card}
            onOpenDetails={onOpenDetails}
            onCopyLink={vi.fn()}
            onDuplicate={vi.fn()}
            onDelete={vi.fn()}
          />
        </SortableContext>
      </DndContext>,
    );
    const root = screen.getByRole("button", { name: card.title });
    fireEvent.click(root);
    fireEvent.keyDown(root, { key: "Enter" });
    fireEvent.keyDown(root, { key: " " });
    expect(onOpenDetails).toHaveBeenCalledTimes(3);
    fireEvent.click(screen.getByRole("button", { name: `Move ${card.title}` }));
    expect(onOpenDetails).toHaveBeenCalledTimes(3);
  });
});

describe("KanbanCard structure", () => {
  it("is one article named by the title, with a clamped two-line title", () => {
    const { card } = renderCard({
      title:
        "A deliberately long card title that must wrap onto two lines and then be clamped without changing its accessible name",
    });
    const article = screen.getByRole("article", { name: card.title });
    expect(screen.getAllByRole("article")).toHaveLength(1);
    const title = within(article).getByText(card.title, { selector: "p" });
    expect(title.className).toContain("line-clamp-2");
    expect(title.textContent).toBe(card.title);
  });

  it("keeps the open control, the grip and the menu as separate named controls", () => {
    const { card, onOpenDetails } = renderCard();
    expect(
      screen.getByRole("button", { name: `Move ${card.title}` }),
    ).toBeTruthy();
    expect(
      screen.getByRole("button", { name: `Open ${card.title} menu` }),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: card.title }));
    expect(onOpenDetails).toHaveBeenCalledWith(card.id);
  });

  it("does not use raw colours, arbitrary text sizes or a raw z-index in its markup", () => {
    const { container } = renderCard({
      priority: "urgent",
      status: "status-working",
      dueDate: fixedIso(-3),
      members: members(2),
      checklists: [twoItemChecklist],
      _count: { comments: 2, attachments: 1, checklistItems: 2 },
    });
    const html = container.innerHTML;
    expect(html).not.toMatch(/#[0-9a-fA-F]{3,8}\b/);
    expect(html).not.toMatch(/rgba?\(/);
    expect(html).not.toMatch(/text-\[\d/);
    expect(html).not.toMatch(/z-index/);
    expect(html).not.toMatch(/aria-grabbed/);
  });

  it("lifts a card dragged with the keyboard on the dropdown stacking role", () => {
    const { card } = renderCard();
    const grip = screen.getByRole("button", { name: `Move ${card.title}` });
    grip.focus();
    fireEvent.keyDown(grip, { code: "Space", key: " " });
    const article = screen.getByRole("article", { name: card.title });
    expect(article.className).toContain("z-dropdown");
    expect(article.getAttribute("style") ?? "").not.toContain("z-index");
  });
});

describe("KanbanCard due date and overdue", () => {
  it("shows an Overdue text marker for a past due date on an open card", () => {
    renderCard({ dueDate: fixedIso(-5) });
    expect(screen.getByText("Overdue")).toBeTruthy();
  });

  it("does not mark a future due date as overdue", () => {
    renderCard({ dueDate: fixedIso(5) });
    expect(screen.queryByText("Overdue")).toBeNull();
    expect(screen.getByText("Due")).toBeTruthy();
  });

  it("does not mark a card due later today as overdue", () => {
    renderCard({ dueDate: NOW.toISOString() });
    expect(screen.queryByText("Overdue")).toBeNull();
  });

  it("does not mark a completed card as overdue", () => {
    renderCard({ dueDate: fixedIso(-5), status: "status-done" });
    expect(screen.queryByText("Overdue")).toBeNull();
  });

  it("does not mark a card with a completion date as overdue", () => {
    renderCard({ dueDate: fixedIso(-5), completedAt: fixedIso(-6) });
    expect(screen.queryByText("Overdue")).toBeNull();
  });

  it("shows nothing for a card without a due date", () => {
    renderCard({ dueDate: undefined });
    expect(screen.queryByText("Overdue")).toBeNull();
    expect(screen.queryByText("Due")).toBeNull();
  });

  it("treats an unparseable due date as no due date instead of throwing", () => {
    renderCard({ dueDate: "not-a-date" });
    expect(screen.queryByText("Overdue")).toBeNull();
    expect(screen.queryByText("Due")).toBeNull();
  });

  it("renders the Vietnamese label from the vi copy", () => {
    renderCard({ dueDate: fixedIso(-5) }, { copy: kanbanCardCopy("vi") });
    expect(screen.getByText("Quá hạn")).toBeTruthy();
    expect(screen.queryByText("Overdue")).toBeNull();
  });

  it("keeps Overdue as text, not colour only, on the danger status role", () => {
    renderCard({ dueDate: fixedIso(-5) });
    const marker = screen.getByText("Overdue").closest("[data-overdue]");
    expect(marker).not.toBeNull();
    expect(marker!.className).toContain("danger-subtle");
  });
});

describe("KanbanCard status", () => {
  it.each([
    ["status-not-started", "Not started"],
    ["status-working", "Working on it"],
    ["status-stuck", "Stuck"],
    ["status-done", "Done"],
    ["status-completed", "Completed"],
  ])("labels the known status %s as %s", (status, label) => {
    renderCard({ status });
    expect(screen.getByText(label)).toBeTruthy();
  });

  it("uses status roles, never identity colours, for the badge", () => {
    renderCard({ status: "status-stuck" });
    const badge = screen.getByText("Stuck");
    expect(badge.className).toContain("danger-subtle");
    expect(badge.className).not.toContain("identity");
  });

  it("localises the status label", () => {
    renderCard({ status: "status-working" }, { copy: kanbanCardCopy("vi") });
    expect(screen.getByText("Đang thực hiện")).toBeTruthy();
  });

  it("renders no badge, and never the raw value, for a status the catalog does not know", () => {
    const { card } = renderCard({ status: "waiting-on-legal" });
    const article = screen.getByRole("article", { name: card.title });
    expect(article.querySelector("[data-slot='badge']")).toBeNull();
    expect(screen.queryByText("waiting-on-legal")).toBeNull();
    expect(article.textContent).not.toContain("waiting-on-legal");
    expect(screen.getByRole("button", { name: card.title })).toBeTruthy();
  });

  it("renders no badge for the fixture default status", () => {
    const { card } = renderCard({ status: "todo" });
    const article = screen.getByRole("article", { name: card.title });
    expect(article.querySelector("[data-slot='badge']")).toBeNull();
    expect(screen.queryByText("todo")).toBeNull();
  });

  it("renders no status badge when the status is empty", () => {
    const { card } = renderCard({ status: "" });
    const article = screen.getByRole("article", { name: card.title });
    expect(article.querySelector("[data-slot='badge']")).toBeNull();
  });
});

describe("KanbanCard assignees", () => {
  it("shows no assignee for a card without members", () => {
    renderCard({ members: [] });
    expect(screen.queryByRole("group", { name: /Assigned to/ })).toBeNull();
  });

  it("exposes the single assignee by name to assistive tech", () => {
    renderCard({ members: members(1) });
    expect(
      screen.getByRole("group", { name: "Assigned to Avery Stone" }),
    ).toBeTruthy();
    expect(screen.queryByText(/^\+\d/)).toBeNull();
  });

  it("shows the first assignee and +N for the rest", () => {
    renderCard({ members: members(3) });
    const group = screen.getByRole("group", {
      name: "Assigned to Avery Stone and 2 more",
    });
    expect(within(group).getByText("Avery Stone")).toBeTruthy();
    expect(within(group).getByText("+2")).toBeTruthy();
  });

  it("handles nine or more assignees without listing them all", () => {
    renderCard({ members: members(12) });
    const group = screen.getByRole("group", {
      name: "Assigned to Avery Stone and 11 more",
    });
    expect(within(group).getByText("+11")).toBeTruthy();
    expect(screen.queryByText("Member 12")).toBeNull();
  });

  it("truncates a long name visually while the accessible name stays complete", () => {
    const longName =
      "Bartholomew Maximilian Featherstonehaugh-Cholmondeley the Third";
    renderCard({
      members: [memberFixture({ name: longName, initials: "BM" })],
    });
    const group = screen.getByRole("group", {
      name: `Assigned to ${longName}`,
    });
    const name = within(group).getByText(longName);
    expect(name.className).toContain("truncate");
  });

  it("does not paint the mark with the raw member colour", () => {
    const { container } = renderCard({
      members: [memberFixture({ color: "#ff0000" })],
    });
    expect(container.innerHTML).not.toContain("#ff0000");
    expect(container.innerHTML).toContain("bg-identity-");
  });

  it("localises the assignee label", () => {
    renderCard({ members: members(2) }, { copy: kanbanCardCopy("vi") });
    expect(
      screen.getByRole("group", {
        name: "Được giao cho Avery Stone và 1 người khác",
      }),
    ).toBeTruthy();
  });
});

describe("KanbanCard priority", () => {
  it.each([
    ["urgent", "Urgent priority"],
    ["high", "High priority"],
  ] as const)(
    "shows %s as an icon with an accessible name",
    (priority, name) => {
      renderCard({ priority });
      const icon = screen.getByRole("img", { name });
      expect(icon.textContent).toBe("");
      expect(screen.queryByText(priority, { exact: false })).toBeNull();
    },
  );

  it.each(["medium", "low"] as const)(
    "does not show %s priority",
    (priority) => {
      renderCard({ priority });
      expect(screen.queryByRole("img")).toBeNull();
    },
  );

  it("localises the priority name", () => {
    renderCard({ priority: "urgent" }, { copy: kanbanCardCopy("vi") });
    expect(screen.getByRole("img", { name: "Ưu tiên khẩn cấp" })).toBeTruthy();
  });
});

describe("KanbanCard quiet metadata", () => {
  it("shows checklist progress, comment and attachment counts", () => {
    renderCard({
      checklists: [twoItemChecklist],
      _count: { comments: 3, attachments: 2, checklistItems: 2 },
    });
    expect(screen.getByText("1 of 2 checklist items done")).toBeTruthy();
    expect(screen.getByText("3 comments")).toBeTruthy();
    expect(screen.getByText("2 attachments")).toBeTruthy();
  });

  it.each([
    [1, "1 comment", "1 attachment"],
    [2, "2 comments", "2 attachments"],
  ])(
    "uses the singular or plural count copy for %i (en)",
    (count, comments, attachments) => {
      renderCard({
        _count: { comments: count, attachments: count, checklistItems: 0 },
      });
      expect(screen.getByText(comments)).toBeTruthy();
      expect(screen.getByText(attachments)).toBeTruthy();
    },
  );

  it("renders no comment or attachment count for 0", () => {
    const { card } = renderCard({
      _count: { comments: 0, attachments: 0, checklistItems: 0 },
    });
    const article = screen.getByRole("article", { name: card.title });
    expect(article.textContent).not.toMatch(/comment|attachment/i);
  });

  it.each([1, 2])("localises the count copy for %i (vi)", (count) => {
    renderCard(
      { _count: { comments: count, attachments: count, checklistItems: 0 } },
      { copy: kanbanCardCopy("vi") },
    );
    expect(screen.getByText(`${count} bình luận`)).toBeTruthy();
    expect(screen.getByText(`${count} tệp đính kèm`)).toBeTruthy();
  });

  it("shows the linked Doc marker", () => {
    renderCard({ linkedPageId: "page-1" });
    expect(screen.getByText("Doc linked")).toBeTruthy();
  });
});

describe("KanbanCard edge data", () => {
  it("renders the worst case together without losing the title", () => {
    const { card } = renderCard({
      title: "Đa ngôn ngữ — 日本語 — a very long title ".repeat(6).trim(),
      status: "a-status-nobody-registered-with-a-long-name",
      priority: "urgent",
      dueDate: fixedIso(-30),
      members: members(9),
      linkedPageId: "page-1",
      _count: { comments: 120, attachments: 45, checklistItems: 0 },
    });
    expect(screen.getByRole("article", { name: card.title })).toBeTruthy();
    expect(screen.getByText("Overdue")).toBeTruthy();
    expect(screen.getByRole("img", { name: "Urgent priority" })).toBeTruthy();
  });
});
