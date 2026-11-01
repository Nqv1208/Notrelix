import { useState } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  timelineDenseScenario,
  timelineEdgeScenario,
  timelineEmptyScenario,
  timelineReadOnlyScenario,
  timelineScenario,
  type TimelineScenario,
} from "../../../../verification/timeline-fixtures";
import type { DayKey } from "../../calendar/calendar-model";
import {
  BoardTimelineSurface,
  type BoardTimelineSurfaceProps,
  type TimelineReschedule,
} from "../board-timeline-surface";
import type { TimelineItem, TimelineScale } from "../timeline-model";
import { timelineCopy } from "../timeline.copy";

const LOCALES = ["vi", "en"] as const;

beforeAll(() => {
  // JSDOM has no PointerEvent: without one the pointer events lose their
  // coordinates and button.
  if (typeof window.PointerEvent === "undefined") {
    class TestPointerEvent extends MouseEvent {
      readonly pointerId: number;
      constructor(type: string, init: PointerEventInit = {}) {
        super(type, init);
        this.pointerId = init.pointerId ?? 1;
      }
    }
    Object.defineProperty(window, "PointerEvent", {
      configurable: true,
      value: TestPointerEvent,
    });
  }
});

interface Spies {
  onOpenItem?: (id: string) => void;
  onReschedule?: (id: string, change: TimelineReschedule) => void;
  onOpenUndated?: () => void;
  onAddItem?: () => void;
}

/** Holds what the container holds: the scale and the window. */
function Host({
  scenario,
  spies,
  extra,
}: {
  scenario: TimelineScenario;
  spies: Spies;
  extra?: Partial<BoardTimelineSurfaceProps>;
}) {
  const [scale, setScale] = useState<TimelineScale>(scenario.scale);
  const [anchorDay, setAnchorDay] = useState<DayKey>(scenario.anchorDay);
  return (
    <div style={{ height: 800 }}>
      <BoardTimelineSurface
        {...scenario}
        scale={scale}
        onScaleChange={setScale}
        anchorDay={anchorDay}
        onAnchorChange={setAnchorDay}
        onOpenItem={(id) => spies.onOpenItem?.(id)}
        onReschedule={(item: TimelineItem, change) =>
          spies.onReschedule?.(item.id, change)
        }
        onOpenUndated={spies.onOpenUndated}
        onAddItem={spies.onAddItem}
        {...extra}
      />
    </div>
  );
}

function renderTimeline(
  scenario: TimelineScenario = timelineScenario(),
  spies: Spies = {},
  extra?: Partial<BoardTimelineSurfaceProps>,
) {
  return renderPureUi(<Host scenario={scenario} spies={spies} extra={extra} />);
}

const track = (container: HTMLElement, id: string) => {
  const element = container.querySelector<HTMLElement>(
    `[data-tl="trow"][data-row-id="${id}"]`,
  );
  if (!element) throw new Error(`no track ${id}`);
  return element;
};

const listRow = (container: HTMLElement, id: string) => {
  const element = container.querySelector<HTMLElement>(
    `[data-tl="lrow"][data-row-id="${id}"]`,
  );
  if (!element) throw new Error(`no row ${id}`);
  return element;
};

const barOf = (container: HTMLElement, id: string) => {
  const element = track(container, id).querySelector<HTMLElement>(
    '[data-tl="gb"]',
  );
  if (!element) throw new Error(`no bar ${id}`);
  return element;
};

const dueOf = (container: HTMLElement, id: string) =>
  listRow(container, id).querySelector<HTMLElement>(":scope > span:last-child")
    ?.textContent;

describe("BoardTimelineSurface", () => {
  it("FUI[wm.timeline.board:scale-switch] draws the day, week and month header and bar", () => {
    for (const locale of LOCALES) {
      const copy = timelineCopy(locale);
      const { container, unmount } = renderTimeline(
        timelineScenario({ locale }),
      );
      expect(container.querySelectorAll('[data-tl="day"]')).toHaveLength(29);
      expect(
        screen
          .getByRole("radio", { name: copy.scale.day })
          .getAttribute("aria-checked"),
      ).toBe("true");
      expect(screen.getByRole("grid").getAttribute("aria-label")).toBe(
        locale === "vi"
          ? "Dòng thời gian Triển khai ERP Q4, 5/10 đến 2/11/2026"
          : "Timeline of Triển khai ERP Q4, 5/10 to 2/11/2026",
      );
      const dayWidth = barOf(container, "erp-146").style.width;

      fireEvent.click(screen.getByRole("radio", { name: copy.scale.week }));
      expect(container.querySelectorAll('[data-tl="day"]')).toHaveLength(0);
      expect(container.querySelectorAll('[data-tl="wk"]')).toHaveLength(6);
      expect(barOf(container, "erp-146").style.width).not.toBe(dayWidth);
      expect(screen.getByRole("grid").getAttribute("aria-label")).toContain(
        locale === "vi" ? "28/9 đến 5/11/2026" : "28/9 to 5/11/2026",
      );

      fireEvent.click(screen.getByRole("radio", { name: copy.scale.month }));
      expect(container.querySelectorAll('[data-tl="mt"]')).toHaveLength(17);
      expect(container.querySelectorAll('[data-tl="wk"]')).toHaveLength(0);
      expect(barOf(container, "erp-146").style.height).toBe("16px");

      fireEvent.click(screen.getByRole("radio", { name: copy.scale.day }));
      expect(container.querySelectorAll('[data-tl="day"]')).toHaveLength(29);
      unmount();
    }
  });

  it("FUI[wm.timeline.board:scale-keyboard] moves along the scale with the arrow keys", () => {
    renderTimeline();
    const day = screen.getByRole("radio", { name: "Ngày" });
    const week = screen.getByRole("radio", { name: "Tuần" });
    day.focus();
    fireEvent.keyDown(day, { key: "ArrowRight" });
    expect(week.getAttribute("aria-checked")).toBe("true");
    expect(document.activeElement).toBe(week);
    fireEvent.keyDown(week, { key: "ArrowLeft" });
    fireEvent.keyDown(day, { key: "ArrowLeft" });
    expect(
      screen.getByRole("radio", { name: "Tháng" }).getAttribute("aria-checked"),
    ).toBe("true");
    // Only the checked radio is in the tab order.
    expect(day.getAttribute("tabindex")).toBe("-1");
  });

  it("FUI[wm.timeline.board:navigation] steps the window and returns to today", () => {
    for (const locale of LOCALES) {
      const copy = timelineCopy(locale);
      const { container, unmount } = renderTimeline(
        timelineScenario({ locale }),
      );
      const first = () =>
        container.querySelector('[data-tl="day"] b')?.textContent;
      expect(first()).toBe("5");
      fireEvent.click(screen.getByRole("button", { name: copy.next }));
      expect(first()).toBe("12");
      fireEvent.click(screen.getByRole("button", { name: copy.previous }));
      fireEvent.click(screen.getByRole("button", { name: copy.previous }));
      expect(first()).toBe("28");
      fireEvent.click(screen.getByRole("button", { name: copy.todayLabel }));
      expect(first()).toBe("5");
      unmount();
    }
  });

  it("FUI[wm.timeline.board:open-item] opens an item from its key, its title and its bar, but not on a modified click", () => {
    const onOpenItem = vi.fn();
    const { container } = renderTimeline(timelineScenario(), { onOpenItem });
    const row = listRow(container, "erp-142");
    fireEvent.click(within(row).getByRole("link", { name: /ERP-142/ }));
    expect(onOpenItem).toHaveBeenLastCalledWith("erp-142");
    fireEvent.click(
      within(row).getByText(
        "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      ),
    );
    expect(onOpenItem).toHaveBeenCalledTimes(2);
    // A new-tab click keeps the link's own meaning.
    fireEvent.click(within(row).getByRole("link", { name: /ERP-142/ }), {
      ctrlKey: true,
    });
    expect(onOpenItem).toHaveBeenCalledTimes(2);
    // A press and release on a bar, without a drag, opens it too.
    const bar = barOf(container, "erp-146");
    fireEvent.pointerDown(bar, { clientX: 100, button: 0, pointerId: 1 });
    fireEvent.pointerUp(bar, { clientX: 100, button: 0, pointerId: 1 });
    expect(onOpenItem).toHaveBeenLastCalledWith("erp-146");
    // Phases have no key and no link.
    const phase = listRow(container, "phase-uat");
    expect(within(phase).queryByRole("link")).toBeNull();
    expect(within(phase).getByText("Giai đoạn")).toBeTruthy();
  });

  it("FUI[wm.timeline.board:bars] names a bar with its span, progress and overdue note", () => {
    const { container } = renderTimeline();
    expect(barOf(container, "erp-142").getAttribute("aria-label")).toBe(
      "ERP-142 Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP, 01/10–10/10, 60%, quá hạn 2 ngày, bắt đầu trước khoảng đang xem",
    );
    expect(track(container, "erp-142").textContent).toContain("Quá hạn 2 ngày");
    expect(dueOf(container, "erp-142")).toBe("10/10");
    expect(
      barOf(container, "erp-142").querySelector<HTMLElement>('[data-tl="pg"]')
        ?.style.width,
    ).toBe("60%");
    // A done item with a finish date before the window is a note, not a bar.
    expect(
      track(container, "erp-129").querySelector('[data-tl="gb"]'),
    ).toBeNull();
    expect(track(container, "erp-129").textContent).toContain(
      "Hoàn thành 30/09",
    );
    // The milestone is a diamond with its date in the name.
    expect(
      screen.getByRole("img", { name: "Mốc Go-live ERP, 01/11" }),
    ).toBeTruthy();
    expect(container.querySelectorAll('[data-tl="dl"]').length).toBe(17);
    expect(container.querySelectorAll('[data-tl="ah"]').length).toBe(7);
  });

  it("FUI[wm.timeline.board:inert-controls] shows what has no data as inert, with a reason", () => {
    const onAddItem = vi.fn();
    for (const locale of LOCALES) {
      const copy = timelineCopy(locale);
      const { unmount } = renderTimeline(
        { ...timelineScenario({ locale }), links: [] },
        {},
      );
      const groupBy = screen.getByRole("button", {
        name: new RegExp(`^${copy.groupBy.label}`),
      });
      const barSpan = screen.getByRole("button", {
        name: new RegExp(`^${copy.barSpan.label}`),
      });
      const links = screen.getByRole("button", {
        name: new RegExp(`^${copy.links}`),
      });
      const critical = screen.getByRole("button", {
        name: new RegExp(`^${copy.criticalPath}`),
      });
      const add = screen.getByRole("button", {
        name: new RegExp(`^${copy.addItem}`),
      });
      const reasons = [
        [groupBy, copy.unavailable.groupBy],
        [barSpan, copy.unavailable.barSpan],
        [links, copy.unavailable.links],
        [critical, copy.unavailable.criticalPath],
        [add, copy.unavailable.addItem],
      ] as const;
      for (const [control, reason] of reasons) {
        expect(control.getAttribute("aria-disabled")).toBe("true");
        expect(control.getAttribute("title")).toBe(reason);
        expect(control.getAttribute("aria-label")).toContain(reason);
        fireEvent.click(control);
      }
      // Dependencies look on, as the design draws them; nothing was toggled.
      expect(links.getAttribute("aria-pressed")).toBe("true");
      expect(critical.getAttribute("aria-pressed")).toBe("false");
      expect(onAddItem).not.toHaveBeenCalled();
      unmount();
    }
  });

  it("FUI[wm.timeline.board:links] toggles the dependency lines and the critical path when they exist", () => {
    const { container } = renderTimeline();
    const links = screen.getByRole("button", { name: "Phụ thuộc" });
    const critical = screen.getByRole("button", { name: "Đường găng" });
    expect(links.getAttribute("aria-disabled")).toBeNull();
    expect(container.querySelectorAll('[data-tl="dl"]').length).toBe(17);
    fireEvent.click(links);
    expect(links.getAttribute("aria-pressed")).toBe("false");
    expect(container.querySelectorAll('[data-tl="dl"]')).toHaveLength(0);
    fireEvent.click(links);
    expect(container.querySelectorAll('[data-tl="dl"]').length).toBe(17);

    expect(critical.getAttribute("aria-pressed")).toBe("false");
    expect(barOf(container, "erp-146").className).not.toContain("opacity-35");
    fireEvent.click(critical);
    expect(critical.getAttribute("aria-pressed")).toBe("true");
    expect(barOf(container, "erp-146").className).toContain("opacity-35");
    expect(barOf(container, "erp-142").className).toContain("ring-2");
    fireEvent.click(critical);
    expect(barOf(container, "erp-146").className).not.toContain("opacity-35");
  });

  it("FUI[wm.timeline.board:critical-path] starts with the path shown when told to", () => {
    const { container } = renderTimeline(
      timelineScenario({ state: "duong-gang" }),
    );
    expect(
      screen
        .getByRole("button", { name: "Đường găng" })
        .getAttribute("aria-pressed"),
    ).toBe("true");
    expect(barOf(container, "erp-142").className).toContain("ring-2");
    expect(barOf(container, "erp-151").className).toContain("opacity-35");
  });

  it("FUI[wm.timeline.board:undated] links the items without a date to where they get one", () => {
    const onOpenUndated = vi.fn();
    for (const locale of LOCALES) {
      const copy = timelineCopy(locale);
      const { unmount } = renderTimeline(timelineScenario({ locale }), {
        onOpenUndated,
      });
      fireEvent.click(
        screen.getByRole("link", {
          name: copy.undated_one.replace("{{count}}", "1"),
        }),
      );
      unmount();
    }
    expect(onOpenUndated).toHaveBeenCalledTimes(2);
    // No count, no link.
    const { unmount } = renderTimeline({
      ...timelineScenario(),
      undatedCount: 0,
    });
    expect(screen.queryByRole("link", { name: /chưa có ngày/ })).toBeNull();
    unmount();
    renderTimeline({ ...timelineScenario(), undatedCount: 3 });
    expect(
      screen.getByRole("link", { name: "3 việc chưa có ngày" }),
    ).toBeTruthy();
  });

  it("FUI[wm.timeline.board:keyboard] moves a bar with Alt and the arrow keys, its due day with Shift", () => {
    const onReschedule = vi.fn();
    const { container } = renderTimeline(timelineScenario(), { onReschedule });
    const title = within(listRow(container, "erp-146")).getAllByRole(
      "link",
    )[1]!;
    expect(title.getAttribute("aria-keyshortcuts")).toContain("Alt+ArrowRight");
    fireEvent.keyDown(title, { key: "ArrowRight" });
    expect(onReschedule).not.toHaveBeenCalled();
    fireEvent.keyDown(title, { key: "ArrowRight", altKey: true });
    expect(onReschedule).toHaveBeenLastCalledWith("erp-146", {
      mode: "move",
      days: 1,
    });
    fireEvent.keyDown(title, {
      key: "ArrowLeft",
      altKey: true,
      shiftKey: true,
    });
    expect(onReschedule).toHaveBeenLastCalledWith("erp-146", {
      mode: "resize",
      days: -1,
    });
    // A phase cannot be moved.
    const phase = listRow(container, "phase-uat");
    expect(within(phase).queryByRole("link")).toBeNull();
  });

  it("FUI[wm.timeline.board:drag-resize] pulls the due day with the handle and reports it on release", () => {
    const onReschedule = vi.fn();
    const { container } = renderTimeline(timelineScenario(), { onReschedule });
    const row = track(container, "erp-146");
    expect(row.querySelector('[data-tl="handle"]')).toBeNull();
    fireEvent.pointerEnter(row);
    const handle = row.querySelector<HTMLElement>('[data-tl="handle"]');
    expect(handle).toBeTruthy();
    if (!handle) return;
    fireEvent.pointerDown(handle, { clientX: 500, button: 0, pointerId: 1 });
    fireEvent.pointerMove(handle, { clientX: 552, pointerId: 1 });
    // 52 px at 26 px a day: two days. The list, ghost and tip follow at once.
    expect(dueOf(container, "erp-146")).toBe("18/10");
    expect(row.querySelector('[data-tl="ghost"]')).toBeTruthy();
    expect(row.querySelector('[data-tl="tip"]')?.textContent).toBe(
      "Hạn: 16/10 → 18/10",
    );
    fireEvent.pointerUp(handle, { clientX: 552, pointerId: 1 });
    expect(onReschedule).toHaveBeenCalledTimes(1);
    expect(onReschedule).toHaveBeenCalledWith("erp-146", {
      mode: "resize",
      days: 2,
    });
    // The surface lets go: the container owns the write.
    expect(dueOf(container, "erp-146")).toBe("16/10");
    expect(row.querySelector('[data-tl="tip"]')).toBeNull();
  });

  it("FUI[wm.timeline.board:drag-move] moves the whole span with the bar", () => {
    const onReschedule = vi.fn();
    const onOpenItem = vi.fn();
    const { container } = renderTimeline(timelineScenario(), {
      onReschedule,
      onOpenItem,
    });
    const bar = barOf(container, "erp-146");
    fireEvent.pointerDown(bar, { clientX: 300, button: 0, pointerId: 1 });
    fireEvent.pointerMove(bar, { clientX: 326, pointerId: 1 });
    expect(
      track(container, "erp-146").querySelector('[data-tl="tip"]')?.textContent,
    ).toBe("13–16/10 → 14–17/10");
    fireEvent.pointerUp(bar, { clientX: 326, pointerId: 1 });
    expect(onReschedule).toHaveBeenCalledWith("erp-146", {
      mode: "move",
      days: 1,
    });
    // A drag is not a click.
    expect(onOpenItem).not.toHaveBeenCalled();
  });

  it("FUI[wm.timeline.board:drag-cancel] puts the bar back on Escape, on cancel and on no movement", () => {
    const onReschedule = vi.fn();
    const { container } = renderTimeline(timelineScenario(), { onReschedule });
    const bar = barOf(container, "erp-146");
    fireEvent.pointerDown(bar, { clientX: 300, button: 0, pointerId: 1 });
    fireEvent.pointerMove(bar, { clientX: 352, pointerId: 1 });
    expect(dueOf(container, "erp-146")).toBe("18/10");
    fireEvent.keyDown(document, { key: "Escape" });
    expect(dueOf(container, "erp-146")).toBe("16/10");
    fireEvent.pointerUp(bar, { clientX: 352, pointerId: 1 });

    fireEvent.pointerDown(bar, { clientX: 300, button: 0, pointerId: 2 });
    fireEvent.pointerMove(bar, { clientX: 352, pointerId: 2 });
    fireEvent.pointerCancel(bar, { pointerId: 2 });
    expect(dueOf(container, "erp-146")).toBe("16/10");

    // Under 13 px is no day: nothing is reported.
    fireEvent.pointerDown(bar, { clientX: 300, button: 0, pointerId: 3 });
    fireEvent.pointerMove(bar, { clientX: 310, pointerId: 3 });
    fireEvent.pointerUp(bar, { clientX: 310, pointerId: 3 });
    expect(onReschedule).not.toHaveBeenCalled();
  });

  it("FUI[wm.timeline.board:drag-limit] cannot pull the due day before the start", () => {
    const onReschedule = vi.fn();
    const { container } = renderTimeline(timelineScenario(), { onReschedule });
    const row = track(container, "erp-146");
    fireEvent.pointerEnter(row);
    const handle = row.querySelector<HTMLElement>('[data-tl="handle"]')!;
    fireEvent.pointerDown(handle, { clientX: 500, button: 0, pointerId: 1 });
    fireEvent.pointerMove(handle, { clientX: 100, pointerId: 1 });
    expect(dueOf(container, "erp-146")).toBe("13/10");
    fireEvent.pointerUp(handle, { clientX: 100, pointerId: 1 });
    expect(onReschedule).toHaveBeenCalledWith("erp-146", {
      mode: "resize",
      days: -3,
    });
  });

  it("FUI[wm.timeline.board:dragging] draws the keo-gian preview: handle, ghost, tip and the pushed item", () => {
    const { container } = renderTimeline(
      timelineScenario({ state: "keo-gian" }),
    );
    expect(container.querySelectorAll('[data-tl="handle"]')).toHaveLength(1);
    expect(container.querySelectorAll('[data-tl="ghost"]')).toHaveLength(2);
    expect(container.querySelector('[data-tl="tip"]')?.textContent).toBe(
      "Hạn: 21/10 → 23/10Đào tạo phòng Kế toán tự lùi 2 ngày, sang 24/10",
    );
    expect(dueOf(container, "erp-147")).toBe("23/10");
    expect(dueOf(container, "phase-training")).toBe("25/10");
  });

  it("FUI[wm.timeline.board:read-only] gives a viewer no add, no handle, no drag and no keyboard move", () => {
    const onReschedule = vi.fn();
    const onOpenItem = vi.fn();
    const { container } = renderTimeline(timelineReadOnlyScenario(), {
      onReschedule,
      onOpenItem,
    });
    expect(
      screen.queryByRole("button", { name: /^Thêm công việc/ }),
    ).toBeNull();
    const row = track(container, "erp-146");
    fireEvent.pointerEnter(row);
    expect(row.querySelector('[data-tl="handle"]')).toBeNull();
    const bar = barOf(container, "erp-146");
    fireEvent.pointerDown(bar, { clientX: 300, button: 0, pointerId: 1 });
    fireEvent.pointerMove(bar, { clientX: 352, pointerId: 1 });
    expect(dueOf(container, "erp-146")).toBe("16/10");
    fireEvent.pointerUp(bar);
    const title = within(listRow(container, "erp-146")).getAllByRole(
      "link",
    )[1]!;
    fireEvent.keyDown(title, { key: "ArrowRight", altKey: true });
    expect(onReschedule).not.toHaveBeenCalled();
    expect(title.getAttribute("aria-keyshortcuts")).toBeNull();
    // Opening an item still works, from the bar too.
    fireEvent.click(bar);
    expect(onOpenItem).toHaveBeenCalledWith("erp-146");
  });

  it("FUI[wm.timeline.board:empty] says no item has a date, and keeps the header", () => {
    const { container } = renderTimeline(timelineEmptyScenario());
    expect(screen.getByRole("status").textContent).toContain(
      "Chưa có việc nào có ngày",
    );
    expect(container.querySelectorAll('[data-tl="day"]')).toHaveLength(29);
    expect(container.querySelectorAll('[data-tl="lrow"]')).toHaveLength(0);
  });

  it("FUI[wm.timeline.board:edge-data] clips bars at the window, notes items outside it and keeps a long title in its cell", () => {
    const { container } = renderTimeline(timelineEdgeScenario());
    expect(track(container, "erp-161").textContent).toContain("");
    expect(barOf(container, "erp-161").className).toContain("rounded-l-none");
    expect(barOf(container, "erp-162").className).toContain("rounded-r-none");
    expect(track(container, "erp-163").textContent).toContain("◂ Hạn 10/09");
    expect(
      track(container, "erp-163").querySelector('[data-tl="gb"]'),
    ).toBeNull();
    expect(track(container, "erp-164").textContent).toContain(
      "Bắt đầu 20/12 ▸",
    );
    // A row without key or assignee still has all four cells.
    expect(
      listRow(container, "bare").querySelectorAll(":scope > span"),
    ).toHaveLength(4);
    const long = listRow(container, "erp-160").querySelector<HTMLElement>(
      ".truncate",
    );
    expect(long?.className).toContain("truncate");
    // No bar leaves the chart.
    const chart = 754;
    for (const bar of container.querySelectorAll<HTMLElement>(
      '[data-tl="gb"]',
    )) {
      expect(
        parseFloat(bar.style.left) + parseFloat(bar.style.width),
      ).toBeLessThanOrEqual(chart);
    }
  });

  it("FUI[wm.timeline.board:high-density] draws every row of a long board inside a scrolling frame", () => {
    const { container } = renderTimeline(timelineDenseScenario());
    expect(container.querySelectorAll('[data-tl="lrow"]')).toHaveLength(42);
    expect(container.querySelectorAll('[data-tl="gb"]')).toHaveLength(42);
    const wrap = container.querySelector<HTMLElement>('[data-tl="wrap"]');
    expect(wrap?.className).toContain("overflow-auto");
    // The header and the list stay put while the rows scroll.
    expect(
      container.querySelector<HTMLElement>('[data-tl="lh"]')?.className,
    ).toContain("sticky");
    expect(
      container.querySelector<HTMLElement>('[data-tl="left"]')?.className,
    ).toContain("sticky");
  });

  it("FUI[wm.timeline.board:legend] names every bar colour, the milestone, the link and today", () => {
    for (const locale of LOCALES) {
      const copy = timelineCopy(locale);
      const { container, unmount } = renderTimeline(
        timelineScenario({ locale }),
      );
      const legend = container.querySelector('[data-tl="legend"]');
      for (const text of [
        copy.legend.todo,
        copy.legend.doing,
        copy.legend.review,
        copy.legend.done,
        copy.legend.overdue,
        copy.legend.milestone,
        copy.legend.link,
      ]) {
        expect(legend?.textContent).toContain(text);
      }
      expect(legend?.textContent).toContain(
        copy.legend.today
          .replace("{{date}}", "12/10")
          .replace("{{zone}}", copy.zone.hanoi),
      );
      unmount();
    }
  });
});

describe("BoardTimelineSurface interactions (W5)", () => {
  const connectorOf = (container: HTMLElement, id: string) => {
    const element = track(container, id).querySelector<HTMLElement>(
      '[data-tl="connector"]',
    );
    if (!element) throw new Error(`no connector ${id}`);
    return element;
  };
  /** The row under the pointer: JSDOM has no layout, so the test names it. */
  const pointerOver = (container: HTMLElement, id: string | null) => {
    document.elementFromPoint = () => (id ? track(container, id) : null);
  };

  it("FUI[wm.timeline.board:zoom-fit] fits the dated items and dims Hôm nay at today", () => {
    for (const locale of LOCALES) {
      const copy = timelineCopy(locale);
      const { unmount } = renderTimeline(timelineScenario({ locale }));
      const today = screen.getByRole("button", { name: copy.todayLabel });
      expect(today.getAttribute("aria-current")).toBe("date");
      fireEvent.click(screen.getByRole("button", { name: copy.next }));
      expect(today.getAttribute("aria-current")).toBeNull();
      fireEvent.click(today);
      expect(today.getAttribute("aria-current")).toBe("date");
      const fit = screen.getByRole("button", { name: copy.fit.label });
      expect(fit.getAttribute("title")).toContain(copy.fit.tip);
      // Without width the whole range only fits the month scale.
      fireEvent.click(fit);
      expect(
        screen
          .getByRole("radio", { name: copy.scale.month })
          .getAttribute("aria-checked"),
      ).toBe("true");
      unmount();
    }
  });

  it("FUI[wm.timeline.board:link-drag] drags a connector onto a bar and refuses a loop before the drop", () => {
    for (const locale of LOCALES) {
      const created: Array<{ from: string; to: string }> = [];
      const { container, unmount } = renderTimeline(
        timelineScenario({ locale }),
        {},
        { onCreateLink: (link) => created.push(link) },
      );
      const connector = connectorOf(container, "erp-150");
      fireEvent.pointerDown(connector, { clientX: 10, clientY: 10, button: 0 });
      pointerOver(container, "erp-147");
      fireEvent.pointerMove(connector, { clientX: 60, clientY: 40 });
      const target = container.querySelector('[data-tl="linktarget"]');
      expect(target?.getAttribute("data-state")).toBe("ok");
      expect(
        within(track(container, "erp-147")).getByRole("status").textContent,
      ).toContain(
        timelineCopy(locale)
          .link.dropTarget.replace("{{to}}", "ERP-147")
          .replace("{{from}}", "ERP-150"),
      );
      fireEvent.pointerUp(connector, { clientX: 60, clientY: 40 });
      expect(created).toEqual([{ from: "erp-150", to: "erp-147" }]);

      // ERP-150 already waits (through ERP-146) for ERP-142: 150 -> 142 is a loop.
      fireEvent.pointerDown(connector, { clientX: 10, clientY: 10, button: 0 });
      pointerOver(container, "erp-142");
      fireEvent.pointerMove(connector, { clientX: 60, clientY: 40 });
      expect(
        container
          .querySelector('[data-tl="linktarget"]')
          ?.getAttribute("data-state"),
      ).toBe("cycle");
      fireEvent.pointerUp(connector, { clientX: 60, clientY: 40 });
      expect(created).toHaveLength(1);
      unmount();
    }
  });

  it("FUI[wm.timeline.board:link-dialog] links from the keyboard and unlinks in the dialog", () => {
    const created: Array<{ from: string; to: string }> = [];
    const removed: Array<{ from: string; to: string }> = [];
    const { container } = renderTimeline(
      timelineScenario(),
      {},
      {
        onCreateLink: (link) => created.push(link),
        onRemoveLink: (link) => removed.push(link),
      },
    );
    fireEvent.click(connectorOf(container, "erp-146"));
    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByText("Liên kết với…")).toBeTruthy();
    // "chờ…" (default): ERP-146 waits for the chosen item.
    fireEvent.change(within(dialog).getByRole("combobox"), {
      target: { value: "ERP-151" },
    });
    fireEvent.click(within(dialog).getByRole("option", { name: /ERP-151/ }));
    fireEvent.click(
      within(dialog).getByRole("button", { name: "Tạo liên kết" }),
    );
    expect(created).toEqual([{ from: "erp-151", to: "erp-146" }]);

    fireEvent.click(connectorOf(container, "erp-146"));
    const again = screen.getByRole("dialog");
    fireEvent.click(
      within(again).getByRole("button", {
        name: /Gỡ liên kết ERP-146 chờ ERP-142/,
      }),
    );
    expect(removed).toEqual([{ from: "erp-142", to: "erp-146" }]);
  });

  it("FUI[wm.timeline.board:link-cycle] disables a row that would close a loop and announces the chain", () => {
    for (const locale of LOCALES) {
      const created: unknown[] = [];
      const { container, unmount } = renderTimeline(
        timelineScenario({ locale }),
        {},
        { onCreateLink: (link) => created.push(link) },
      );
      // ERP-142 waits for ERP-150 would loop: 142 -> 146 -> 150 -> 142.
      fireEvent.click(connectorOf(container, "erp-142"));
      const dialog = screen.getByRole("dialog");
      const row = within(dialog).getByRole("option", { name: /ERP-150/ });
      expect(row.getAttribute("aria-disabled")).toBe("true");
      fireEvent.click(row);
      expect(within(dialog).getByRole("alert").textContent).toContain(
        "ERP-150 → ERP-142 → ERP-146 → ERP-150",
      );
      const create = within(dialog)
        .getAllByRole("button")
        .find(
          (button) => button.textContent === timelineCopy(locale).link.create,
        );
      expect((create as HTMLButtonElement).disabled).toBe(true);
      expect(created).toHaveLength(0);
      unmount();
    }
  });

  /** JSDOM has no DragEvent: the event gets its x by hand. */
  const dragAt = (
    element: HTMLElement,
    type: "dragOver" | "drop",
    clientX: number,
    dataTransfer: object,
  ) => {
    const event = new Event(type.toLowerCase(), {
      bubbles: true,
      cancelable: true,
    });
    Object.assign(event, { clientX, dataTransfer });
    fireEvent(element, event);
  };

  it("FUI[wm.timeline.board:tray] opens over the chart, schedules by date and by drop", () => {
    const scheduled: Array<[string, string]> = [];
    const undatedItems = [
      { id: "erp-153", itemKey: "ERP-153", title: "Lập kế hoạch cắt chuyển" },
    ];
    const { container } = renderTimeline(
      timelineScenario(),
      {},
      { undatedItems, onSchedule: (id, day) => scheduled.push([id, day]) },
    );
    expect(container.querySelector('[data-tl="tray-panel"]')).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: "Mở khay Chưa lên lịch" }),
    );
    const tray = screen.getByRole("region", { name: "Chưa lên lịch" });
    fireEvent.click(
      within(tray).getByRole("button", { name: "Đặt ngày cho ERP-153" }),
    );
    fireEvent.change(within(tray).getByLabelText("Đặt ngày cho ERP-153"), {
      target: { value: "2026-10-15" },
    });
    expect(scheduled).toEqual([["erp-153", "2026-10-15"]]);

    // A drop on the chart: the window starts 5/10 and a day is 26px wide.
    const chip = container.querySelector<HTMLElement>(
      '[data-tl-undated="erp-153"]',
    )!;
    const transfer = { setData: () => undefined, effectAllowed: "" };
    fireEvent.dragStart(chip, { dataTransfer: transfer });
    const body = container.querySelector<HTMLElement>('[data-tl="body"]')!;
    dragAt(body, "dragOver", 26 * 10 + 5, transfer);
    expect(container.querySelector('[data-tl="trayghost"]')).not.toBeNull();
    expect(container.querySelector('[data-tl="tip"]')?.textContent).toContain(
      "ERP-153 vào 15/10",
    );
    dragAt(body, "drop", 26 * 10 + 5, transfer);
    expect(scheduled.at(-1)).toEqual(["erp-153", "2026-10-15"]);
  });

  it("FUI[wm.timeline.board:link-read-only] shows the connectors disabled with the reason and no tray drag", () => {
    const { container } = renderTimeline(
      timelineReadOnlyScenario(),
      {},
      {
        onCreateLink: () => undefined,
        undatedItems: [
          { id: "erp-153", itemKey: "ERP-153", title: "Lập kế hoạch" },
        ],
        onSchedule: () => undefined,
      },
    );
    const connector = connectorOf(container, "erp-146");
    expect(connector.getAttribute("aria-disabled")).toBe("true");
    expect(connector.getAttribute("title")).toBe(
      "Bạn chỉ có quyền xem board này",
    );
    fireEvent.click(connector);
    expect(screen.queryByRole("dialog")).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: "Mở khay Chưa lên lịch" }),
    );
    expect(
      container
        .querySelector('[data-tl-undated="erp-153"]')
        ?.getAttribute("draggable"),
    ).toBe("false");
    expect(screen.getByText(/không thể đặt lịch/)).toBeTruthy();
  });
});
