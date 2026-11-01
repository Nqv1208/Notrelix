import { useState } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  CALENDAR_UNDATED,
  calendarDenseScenario,
  calendarEmptyScenario,
  calendarMockupEntries,
  calendarReadOnlyScenario,
  calendarScenario,
  type CalendarScenario,
} from "../../../../verification/calendar-fixtures";
import {
  BoardCalendarSurface,
  type BoardCalendarSurfaceProps,
} from "../board-calendar-surface";
import { calendarCopy } from "../calendar.copy";
import type {
  CalendarDateBasis,
  CalendarRange,
  DayKey,
} from "../calendar-model";

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

/** The range heading of the bar (the aside has headings of its own). */
const heading = () => screen.getAllByRole("heading", { level: 2 })[0]!;

const dataTransfer = () => ({
  setData: vi.fn(),
  effectAllowed: "",
  dropEffect: "",
});

/** Holds what the container holds: range, day, basis and filter. */
function Host({
  scenario,
  spies,
  extra,
}: {
  extra?: Partial<BoardCalendarSurfaceProps>;
  scenario: CalendarScenario;
  spies: {
    onOpenItem?: (id: string) => void;
    onReschedule?: (id: string, toDay: DayKey) => void;
    onSetDue?: (id: string, day: DayKey) => void;
    onCreateItem?: (title: string, day: DayKey) => void;
    onClearFilter?: () => void;
  };
}) {
  const [range, setRange] = useState<CalendarRange>(scenario.range);
  const [anchorDay, setAnchorDay] = useState<DayKey>(scenario.anchorDay);
  const [dateBasis, setDateBasis] = useState<CalendarDateBasis>(
    scenario.dateBasis,
  );
  return (
    <div style={{ height: 800 }}>
      <BoardCalendarSurface
        {...scenario}
        range={range}
        onRangeChange={setRange}
        anchorDay={anchorDay}
        onAnchorChange={setAnchorDay}
        dateBasis={dateBasis}
        onDateBasisChange={setDateBasis}
        onClearFilter={() => spies.onClearFilter?.()}
        onOpenItem={(id) => spies.onOpenItem?.(id)}
        onReschedule={(entry, toDay) => spies.onReschedule?.(entry.id, toDay)}
        onSetDue={(id, day) => spies.onSetDue?.(id, day)}
        onCreateItem={(title, day) => spies.onCreateItem?.(title, day)}
        {...extra}
      />
    </div>
  );
}

function renderCalendar(
  scenario: CalendarScenario = calendarScenario(),
  spies: Parameters<typeof Host>[0]["spies"] = {},
  extra?: Partial<BoardCalendarSurfaceProps>,
) {
  return renderPureUi(<Host scenario={scenario} spies={spies} extra={extra} />);
}

/** The week row that starts on `monday`. */
function weekOf(container: HTMLElement, monday: DayKey): HTMLElement {
  const week = container.querySelector<HTMLElement>(
    `[data-calendar-week="${monday}"]`,
  );
  if (!week) throw new Error(`no week ${monday}`);
  return week;
}

function dayCell(container: HTMLElement, day: DayKey): HTMLElement {
  const cell = container.querySelector<HTMLElement>(`[data-day="${day}"]`);
  if (!cell) throw new Error(`no day ${day}`);
  return cell;
}

function eventOf(container: HTMLElement, id: string): HTMLElement {
  const chip = container.querySelector<HTMLElement>(
    `[data-calendar-event="${id}"]`,
  );
  if (!chip) throw new Error(`no event ${id}`);
  return chip;
}

describe("BoardCalendarSurface", () => {
  it("FUI[wm.calendar.board:month] draws October 2026 with today, spans, phases and the milestone", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const { container, unmount } = renderCalendar(
        calendarScenario({ locale }),
      );

      expect(heading().textContent).toBe(
        locale === "vi" ? "Tháng 10, 2026" : "October 2026",
      );
      expect(screen.getAllByRole("columnheader")).toHaveLength(7);
      expect(screen.getAllByRole("columnheader")[0]?.textContent).toBe(
        copy.weekdays.mon,
      );
      expect(container.querySelectorAll("[data-calendar-week]")).toHaveLength(
        5,
      );
      expect(screen.getByRole("grid").getAttribute("aria-label")).toBe(
        locale === "vi"
          ? "Lịch tháng 10 năm 2026"
          : "Calendar for October 2026",
      );

      // Today is marked on 12/10 and nowhere else.
      const today = dayCell(container, "2026-10-12");
      expect(today.getAttribute("aria-current")).toBe("date");
      expect(within(today).getByText(copy.todayMarker)).toBeTruthy();
      expect(container.querySelectorAll("[aria-current=date]")).toHaveLength(1);

      // A single-day item sits in its column, a span covers its columns.
      const week = weekOf(container, "2026-10-12");
      expect(eventOf(week, "erp-138").style.gridColumn).toBe("3 / span 1");
      expect(
        eventOf(weekOf(container, "2026-10-05"), "erp-142").style.gridColumn,
      ).toBe("1 / span 6");
      expect(
        eventOf(weekOf(container, "2026-09-28"), "erp-142").style.gridColumn,
      ).toBe("4 / span 4");

      // Phases and the milestone are markers, not buttons.
      const markers = container.querySelectorAll("[data-calendar-marker]");
      expect(
        Array.from(markers).map((marker) =>
          marker.getAttribute("data-calendar-marker"),
        ),
      ).toEqual(["phase", "phase", "phase", "milestone"]);
      expect(screen.getAllByText(copy.kind.phase)).toHaveLength(3);
      expect(screen.getByText(copy.kind.milestone)).toBeTruthy();

      // The aside: one undated item and the legend.
      expect(
        screen.getByRole("complementary", { name: copy.undated.region }),
      ).toBeTruthy();
      expect(screen.getByText(copy.legend.title)).toBeTruthy();
      expect(screen.getByText("ERP-153")).toBeTruthy();
      unmount();
    }
  });

  it("FUI[wm.calendar.board:event] names an entry with its note and opens it on click", () => {
    const onOpenItem = vi.fn();
    const { container } = renderCalendar(calendarScenario(), { onOpenItem });
    const chip = eventOf(container, "erp-142");
    expect(chip.getAttribute("aria-label")).toBe(
      "ERP-142 Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP, bắt đầu 01/10, hạn 10/10, quá hạn 2 ngày",
    );
    fireEvent.click(eventOf(weekOf(container, "2026-10-12"), "erp-144"));
    expect(onOpenItem).toHaveBeenCalledWith("erp-144");
  });

  // Heavy interaction sequence: many role queries in jsdom exceed the 5 s default on a loaded runner.
  it("FUI[wm.calendar.board:navigation] steps, returns to today and switches the range", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const { unmount } = renderCalendar(calendarScenario({ locale }));

      fireEvent.click(
        screen.getByRole("button", { name: copy.previous.month }),
      );
      expect(heading().textContent).toBe(
        locale === "vi" ? "Tháng 9, 2026" : "September 2026",
      );
      fireEvent.click(screen.getByRole("button", { name: copy.next.month }));
      fireEvent.click(screen.getByRole("button", { name: copy.next.month }));
      expect(heading().textContent).toBe(
        locale === "vi" ? "Tháng 11, 2026" : "November 2026",
      );
      fireEvent.click(screen.getByRole("button", { name: copy.today }));
      expect(heading().textContent).toBe(
        locale === "vi" ? "Tháng 10, 2026" : "October 2026",
      );

      fireEvent.click(screen.getByRole("radio", { name: copy.ranges.week }));
      expect(heading().textContent).toBe("12/10 – 18/10/2026");
      fireEvent.click(screen.getByRole("button", { name: copy.next.week }));
      expect(heading().textContent).toBe("19/10 – 25/10/2026");

      fireEvent.click(screen.getByRole("radio", { name: copy.ranges.day }));
      expect(heading().textContent).toBe(
        locale === "vi" ? "Thứ Hai, 19/10/2026" : "Monday, 19/10/2026",
      );
      expect(
        screen.getByRole("button", { name: copy.previous.day }),
      ).toBeTruthy();
      unmount();
    }
  }, 20_000);

  it("FUI[wm.calendar.board:range-switch] is a radio group the arrow keys walk", () => {
    renderCalendar();
    const group = screen.getByRole("radiogroup", { name: "Khoảng hiển thị" });
    const [month, week, day] = within(group).getAllByRole("radio");
    expect(month?.getAttribute("aria-checked")).toBe("true");
    expect(month?.getAttribute("tabindex")).toBe("0");
    expect(week?.getAttribute("tabindex")).toBe("-1");
    fireEvent.keyDown(month!, { key: "ArrowRight" });
    expect(week?.getAttribute("aria-checked")).toBe("true");
    fireEvent.keyDown(week!, { key: "ArrowRight" });
    expect(day?.getAttribute("aria-checked")).toBe("true");
    fireEvent.keyDown(day!, { key: "ArrowRight" });
    expect(month?.getAttribute("aria-checked")).toBe("true");
    fireEvent.keyDown(month!, { key: "ArrowLeft" });
    expect(day?.getAttribute("aria-checked")).toBe("true");
  });

  it("FUI[wm.calendar.board:week] draws one week whose entries wrap", () => {
    const { container } = renderCalendar(calendarScenario({ range: "week" }));
    expect(container.querySelectorAll("[data-calendar-week]")).toHaveLength(1);
    expect(heading().textContent).toBe("12/10 – 18/10/2026");
    // The UAT phase spans Monday to Friday; the title is not truncated.
    const phase = container.querySelector<HTMLElement>(
      "[data-calendar-marker=phase]",
    );
    expect(phase?.style.gridColumn).toBe("1 / span 5");
    expect(phase?.querySelector(".truncate")).toBeNull();
  });

  it("FUI[wm.calendar.board:day] lists overdue, all-day, hour slots and the now line", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const { container, unmount } = renderCalendar(
        calendarScenario({ range: "day", locale }),
      );
      expect(heading().textContent).toBe(
        locale === "vi" ? "Thứ Hai, 12/10/2026" : "Monday, 12/10/2026",
      );
      const sections = screen.getAllByRole("row");
      const overdue = sections.find((row) =>
        within(row).queryByText(copy.agenda.overdue),
      );
      expect(overdue).toBeTruthy();
      expect(within(overdue!).getByText("ERP-142")).toBeTruthy();
      expect(
        within(overdue!).getByText(
          locale === "vi" ? "Quá hạn 2 ngày" : "2 days overdue",
        ),
      ).toBeTruthy();
      const allDay = sections.find((row) =>
        within(row).queryByText(copy.agenda.allDay),
      );
      expect(
        within(allDay!).getByText(
          locale === "vi"
            ? "12/10 → 16/10 · đang diễn ra"
            : "12/10 → 16/10 · in progress",
        ),
      ).toBeTruthy();

      expect(screen.getAllByRole("rowheader")).toHaveLength(11);
      expect(screen.getByText("08:00")).toBeTruthy();
      expect(screen.getByText("18:00")).toBeTruthy();
      const slot = screen.getByText("17:00", {
        selector: "time",
      }).parentElement;
      expect(within(slot!).getByText("ERP-144")).toBeTruthy();
      expect(
        within(slot!).getByText(locale === "vi" ? "Hạn 17:00" : "Due 17:00"),
      ).toBeTruthy();

      const now = container.querySelector<HTMLElement>("[data-calendar-now]");
      expect(Math.round(parseFloat(now?.style.top ?? "0"))).toBe(117);
      expect(now?.getAttribute("aria-label")).toBe(
        locale === "vi" ? "Bây giờ 10:40" : "Now 10:40",
      );
      unmount();
    }
  });

  it("FUI[wm.calendar.board:drag] shows where the item lands and reschedules on drop", () => {
    const onReschedule = vi.fn();
    const { container } = renderCalendar(calendarScenario(), { onReschedule });
    const chip = eventOf(weekOf(container, "2026-10-12"), "erp-146");
    expect(chip.getAttribute("draggable")).toBe("true");

    fireEvent.dragStart(chip, { dataTransfer: dataTransfer() });
    // The chip stays where it was, dimmed.
    expect(chip.className).toContain("opacity-40");

    const target = dayCell(container, "2026-10-20");
    fireEvent.dragOver(target, { dataTransfer: dataTransfer() });
    expect(
      container.querySelector("[data-calendar-day-bg='2026-10-20']")?.className,
    ).toContain("bg-state-selected");
    expect(
      container.querySelector("[data-calendar-ghost]")?.textContent,
    ).toContain("ERP-146");
    expect(screen.getByText("Đổi hạn: 16/10 → 20/10")).toBeTruthy();

    fireEvent.drop(target, { dataTransfer: dataTransfer() });
    expect(onReschedule).toHaveBeenCalledWith("erp-146", "2026-10-20");
    expect(container.querySelector("[data-calendar-ghost]")).toBeNull();
    expect(container.querySelector("[data-calendar-tip]")).toBeNull();
  });

  it("FUI[wm.calendar.board:drag-cancel] drops nothing on its own day and clears on drag end", () => {
    const onReschedule = vi.fn();
    const { container } = renderCalendar(calendarScenario(), { onReschedule });
    const chip = eventOf(weekOf(container, "2026-10-12"), "erp-146");
    fireEvent.dragStart(chip, { dataTransfer: dataTransfer() });
    const own = dayCell(container, "2026-10-16");
    fireEvent.dragOver(own, { dataTransfer: dataTransfer() });
    // Over its own day there is no ghost and no tooltip.
    expect(container.querySelector("[data-calendar-ghost]")).toBeNull();
    fireEvent.drop(own, { dataTransfer: dataTransfer() });
    expect(onReschedule).not.toHaveBeenCalled();

    fireEvent.dragStart(chip, { dataTransfer: dataTransfer() });
    fireEvent.dragOver(dayCell(container, "2026-10-20"), {
      dataTransfer: dataTransfer(),
    });
    fireEvent.dragEnd(chip);
    expect(container.querySelector("[data-calendar-ghost]")).toBeNull();
    expect(chip.className).not.toContain("opacity-40");
  });

  it("FUI[wm.calendar.board:keyboard] moves a due date with Alt and the arrows and walks the days", () => {
    const onReschedule = vi.fn();
    const { container } = renderCalendar(calendarScenario(), { onReschedule });
    const chip = eventOf(weekOf(container, "2026-10-12"), "erp-146");
    fireEvent.keyDown(chip, { key: "ArrowRight" });
    expect(onReschedule).not.toHaveBeenCalled();
    fireEvent.keyDown(chip, { key: "ArrowRight", altKey: true });
    expect(onReschedule).toHaveBeenLastCalledWith("erp-146", "2026-10-17");
    fireEvent.keyDown(chip, { key: "ArrowDown", altKey: true });
    expect(onReschedule).toHaveBeenLastCalledWith("erp-146", "2026-10-23");
    fireEvent.keyDown(chip, { key: "ArrowLeft", altKey: true });
    expect(onReschedule).toHaveBeenLastCalledWith("erp-146", "2026-10-15");

    // The anchor day is the one tab stop; arrows move it.
    expect(dayCell(container, "2026-10-12").getAttribute("tabindex")).toBe("0");
    expect(dayCell(container, "2026-10-13").getAttribute("tabindex")).toBe(
      "-1",
    );
    fireEvent.keyDown(dayCell(container, "2026-10-12"), { key: "ArrowRight" });
    expect(dayCell(container, "2026-10-13").getAttribute("tabindex")).toBe("0");
    fireEvent.keyDown(dayCell(container, "2026-10-13"), { key: "ArrowDown" });
    expect(dayCell(container, "2026-10-20").getAttribute("tabindex")).toBe("0");
  });

  it("FUI[wm.calendar.board:undated] sets a due date by drag or by the button", () => {
    const onSetDue = vi.fn();
    const { container } = renderCalendar(calendarScenario(), { onSetDue });
    const item = container.querySelector<HTMLElement>(
      "[data-calendar-undated='erp-153']",
    )!;
    expect(item.getAttribute("draggable")).toBe("true");
    fireEvent.dragStart(item, { dataTransfer: dataTransfer() });
    const target = dayCell(container, "2026-10-21");
    fireEvent.dragOver(target, { dataTransfer: dataTransfer() });
    expect(screen.getByText("Đặt hạn: 21/10")).toBeTruthy();
    fireEvent.drop(target, { dataTransfer: dataTransfer() });
    expect(onSetDue).toHaveBeenLastCalledWith("erp-153", "2026-10-21");

    // The button gives the anchor day (today until the view is moved).
    fireEvent.click(
      screen.getByRole("button", { name: "Đặt ngày cho ERP-153" }),
    );
    expect(onSetDue).toHaveBeenLastCalledWith("erp-153", "2026-10-12");
    fireEvent.click(screen.getByRole("button", { name: "Tháng sau" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Đặt ngày cho ERP-153" }),
    );
    expect(onSetDue).toHaveBeenLastCalledWith("erp-153", "2026-11-12");
  });

  it("FUI[wm.calendar.board:quick-create] opens on a day, creates on Enter and closes on Escape", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const onCreateItem = vi.fn();
      const { container, unmount } = renderCalendar(
        calendarScenario({ locale }),
        { onCreateItem },
      );
      fireEvent.click(dayCell(container, "2026-10-22"));
      const input = screen.getByRole("textbox", {
        name: copy.quickCreate.label.replace("{{date}}", "22/10"),
      });
      expect(input.getAttribute("placeholder")).toBe(
        copy.quickCreate.placeholder,
      );
      expect(
        screen.getByText(copy.quickCreate.hint.replace("{{date}}", "22/10")),
      ).toBeTruthy();
      // Under the popover's week, in the column of 22/10.
      expect(
        weekOf(container, "2026-10-19").querySelector<HTMLElement>(
          "[data-calendar-quick-create]",
        )?.style.gridColumn,
      ).toBe("4 / span 1");

      fireEvent.change(input, { target: { value: "  Soạn biên bản  " } });
      fireEvent.submit(input.closest("form")!);
      expect(onCreateItem).toHaveBeenCalledWith("Soạn biên bản", "2026-10-22");
      expect(
        container.querySelector("[data-calendar-quick-create]"),
      ).toBeNull();

      // An empty title creates nothing; Escape closes.
      fireEvent.click(dayCell(container, "2026-10-23"));
      fireEvent.submit(
        container.querySelector("[data-calendar-quick-create]")!,
      );
      expect(onCreateItem).toHaveBeenCalledTimes(1);
      fireEvent.keyDown(screen.getByRole("textbox"), { key: "Escape" });
      expect(
        container.querySelector("[data-calendar-quick-create]"),
      ).toBeNull();
      unmount();
    }
  });

  it("FUI[wm.calendar.board:add] opens quick create on the anchor day, in the grid and in the day view", () => {
    const onCreateItem = vi.fn();
    const { container } = renderCalendar(calendarScenario(), { onCreateItem });
    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    expect(screen.getByText("Enter để tạo · hạn 12/10")).toBeTruthy();
    fireEvent.keyDown(screen.getByRole("textbox"), { key: "Escape" });

    fireEvent.click(screen.getByRole("radio", { name: "Ngày" }));
    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "Gọi nhà cung cấp" } });
    fireEvent.submit(input.closest("form")!);
    expect(onCreateItem).toHaveBeenCalledWith("Gọi nhà cung cấp", "2026-10-12");
    expect(container.querySelector("[data-calendar-quick-create]")).toBeNull();
  });

  it("FUI[wm.calendar.board:overflow] collapses a crowded day into +N and opens that day", () => {
    renderCalendar(calendarDenseScenario());
    // 14/10 has the UAT phase, the review item and five more: two rows stay
    // and "+5" counts the rest; 20/10 has four items: "+2".
    const more = screen.getByRole("button", {
      name: "5 việc nữa trong ngày 14/10, mở ngày này",
    });
    expect(more.textContent).toBe("+5 việc");
    expect(
      screen.getByRole("button", {
        name: "2 việc nữa trong ngày 20/10, mở ngày này",
      }).textContent,
    ).toBe("+2 việc");
    fireEvent.click(more);
    expect(heading().textContent).toBe("Thứ Tư, 14/10/2026");
    expect(
      screen.getByRole("radio", { name: "Ngày" }).getAttribute("aria-checked"),
    ).toBe("true");
  });

  it("FUI[wm.calendar.board:date-basis] orders by start-to-due or by due day alone", () => {
    const { container } = renderCalendar();
    expect(
      eventOf(weekOf(container, "2026-10-05"), "erp-142").style.gridColumn,
    ).toBe("1 / span 6");
    fireEvent.keyDown(screen.getByRole("button", { name: /Xếp theo/ }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Hạn" }));
    expect(
      screen.getByRole("button", { name: /Xếp theo/ }).textContent,
    ).toContain("Hạn");
    // Only the due day (10/10, Saturday) keeps the entry.
    expect(
      eventOf(weekOf(container, "2026-10-05"), "erp-142").style.gridColumn,
    ).toBe("6 / span 1");
    expect(
      weekOf(container, "2026-09-28").querySelector(
        "[data-calendar-event='erp-142']",
      ),
    ).toBeNull();
  });

  it("FUI[wm.calendar.board:filter] shows no item when the filter hides them all and clears the filter from the no-results card", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const onClearFilter = vi.fn();
      const scenario = calendarScenario({ state: "khong-ket-qua", locale });
      const { container, unmount } = renderCalendar(scenario, {
        onClearFilter,
      });
      // No item is shown; the card explains and clears the filter.
      expect(container.querySelector("[data-calendar-event]")).toBeNull();
      const status = screen.getByRole("status");
      expect(within(status).getByText(copy.noResults.title)).toBeTruthy();
      fireEvent.click(
        within(status).getByRole("button", { name: copy.noResults.clear }),
      );
      expect(onClearFilter).toHaveBeenCalledOnce();
      unmount();
    }
  });

  it("FUI[wm.calendar.board:no-results] says the hidden undated item is hidden by the filter", () => {
    renderCalendar(calendarScenario({ state: "khong-ket-qua" }));
    expect(screen.getByText("1 việc chưa có ngày")).toBeTruthy();
    expect(
      screen.getByText("Việc này không khớp bộ lọc nên đang ẩn."),
    ).toBeTruthy();
    expect(screen.queryByText("ERP-153")).toBeNull();
  });

  it("FUI[wm.calendar.board:read-only] offers no add, drag, quick create or due date to a viewer", () => {
    const onCreateItem = vi.fn();
    const { container } = renderCalendar(calendarReadOnlyScenario(), {
      onCreateItem,
    });
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    expect(
      eventOf(weekOf(container, "2026-10-12"), "erp-146").getAttribute(
        "draggable",
      ),
    ).toBe("false");
    expect(
      screen.queryByRole("button", { name: "Đặt ngày cho ERP-153" }),
    ).toBeNull();
    expect(
      container
        .querySelector("[data-calendar-undated]")
        ?.getAttribute("draggable"),
    ).toBe("false");
    expect(screen.queryByText("Kéo vào lịch hoặc bấm Đặt hạn.")).toBeNull();
    fireEvent.click(dayCell(container, "2026-10-22"));
    expect(container.querySelector("[data-calendar-quick-create]")).toBeNull();
    // Opening an item stays possible.
    expect(eventOf(container, "erp-146")).toBeTruthy();
  });

  it("FUI[wm.calendar.board:empty] draws empty weeks and says every item has a date", () => {
    const { container } = renderCalendar(calendarEmptyScenario());
    expect(container.querySelector("[data-calendar-event]")).toBeNull();
    expect(container.querySelector("[data-calendar-marker]")).toBeNull();
    expect(screen.getByText("Mọi việc đều có ngày")).toBeTruthy();
    // No filter, so no no-results card.
    expect(screen.queryByRole("status")).toBeNull();
  });

  it("FUI[wm.calendar.board:edge-data] truncates a long title and keeps an item without key or assignee", () => {
    expect(calendarMockupEntries().length).toBeGreaterThan(10);
    expect(CALENDAR_UNDATED).toHaveLength(1);
    const base = calendarScenario();
    const { container } = renderCalendar({
      ...base,
      entries: [
        ...base.entries,
        {
          id: "bare",
          kind: "item",
          title: "Việc chưa có mã hay người phụ trách",
          tone: "todo",
          startDay: "2026-10-20",
          endDay: "2026-10-20",
          dueMinutes: null,
          assignee: null,
          note: "hạn 20/10",
          movable: true,
        },
      ],
    });
    const bare = eventOf(container, "bare");
    expect(bare.getAttribute("aria-label")).toBe(
      "Việc chưa có mã hay người phụ trách, hạn 20/10",
    );
    expect(bare.querySelector(".truncate")).toBeTruthy();
    // The mark is optional: only the dot is left.
    expect(bare.querySelectorAll("span[aria-hidden]").length).toBe(1);
  });
});

describe("BoardCalendarSurface interactions (W5)", () => {
  /** A three-day item (14/10 to 16/10) beside the mockup's entries. */
  const span = {
    id: "erp-145",
    kind: "item" as const,
    itemKey: "ERP-145",
    title: "Đào tạo kế toán",
    tone: "todo" as const,
    startDay: "2026-10-14" as DayKey,
    endDay: "2026-10-16" as DayKey,
    note: "",
    movable: true,
  };
  const withSpan = (base = calendarScenario()): CalendarScenario => ({
    ...base,
    entries: [...calendarMockupEntries(), span],
  });
  const handle = (container: HTMLElement, edge: "start" | "due") => {
    const element = container.querySelector<HTMLElement>(
      `[data-calendar-handle="${edge}"][data-entry="erp-145"]`,
    );
    if (!element) throw new Error(`no ${edge} handle`);
    return element;
  };
  /** JSDOM has no layout: week rows are 700px wide and 100px tall, one under the other. */
  const layoutWeeks = (container: HTMLElement) => {
    const rows = [
      ...container.querySelectorAll<HTMLElement>("[data-calendar-week]"),
    ];
    rows.forEach((row, index) => {
      row.getBoundingClientRect = () =>
        ({
          left: 0,
          right: 700,
          top: index * 100,
          bottom: index * 100 + 100,
          width: 700,
          height: 100,
        }) as DOMRect;
    });
    return (monday: DayKey) =>
      rows.findIndex((row) => row.dataset.calendarWeek === monday) * 100 + 50;
  };

  it("FUI[wm.calendar.board:resize] drags the due handle across weeks, tips the new span and saves on release", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const changed: unknown[] = [];
      const { container, unmount } = renderCalendar(
        withSpan(calendarScenario({ locale })),
        {},
        { onChangeDates: (entry, days) => changed.push([entry.id, days]) },
      );
      const y = layoutWeeks(container);
      const due = handle(container, "due");
      fireEvent.pointerDown(due, {
        clientX: 400,
        clientY: y("2026-10-12"),
        button: 0,
        pointerId: 1,
      });
      // The next Monday's column 0 is 19/10: the due day crosses the week break.
      fireEvent.pointerMove(document, {
        clientX: 20,
        clientY: y("2026-10-19"),
        pointerId: 1,
      });
      expect(container.querySelector("[data-calendar-tip]")?.textContent).toBe(
        copy.resizeTip.due.replace("{{to}}", "19/10").replace("{{days}}", "6"),
      );
      fireEvent.pointerUp(document, {
        clientX: 20,
        clientY: y("2026-10-19"),
        pointerId: 1,
      });
      expect(changed).toEqual([
        ["erp-145", { startDay: "2026-10-14", dueDay: "2026-10-19" }],
      ]);
      unmount();
    }
  });

  it("FUI[wm.calendar.board:resize-cancel] keeps the due on the start day and puts the bar back on Escape", () => {
    const changed: unknown[] = [];
    const { container } = renderCalendar(
      withSpan(),
      {},
      {
        onChangeDates: (entry, days) => changed.push([entry.id, days]),
      },
    );
    const y = layoutWeeks(container);
    const due = handle(container, "due");
    fireEvent.pointerDown(due, {
      clientX: 400,
      clientY: y("2026-10-12"),
      button: 0,
      pointerId: 1,
    });
    // 12/10 is before the 14/10 start: the due stops at the start day.
    fireEvent.pointerMove(document, {
      clientX: 20,
      clientY: y("2026-10-12"),
      pointerId: 1,
    });
    fireEvent.keyDown(document, { key: "Escape" });
    fireEvent.pointerUp(document, {
      clientX: 20,
      clientY: y("2026-10-12"),
      pointerId: 1,
    });
    expect(changed).toEqual([]);
    expect(container.querySelector("[data-calendar-tip]")).toBeNull();
  });

  it("FUI[wm.calendar.board:change-dates] changes the dates from the context menu and refuses a due before the start", () => {
    for (const locale of LOCALES) {
      const copy = calendarCopy(locale);
      const changed: unknown[] = [];
      const { container, unmount } = renderCalendar(
        withSpan(calendarScenario({ locale })),
        {},
        { onChangeDates: (entry, days) => changed.push([entry.id, days]) },
      );
      fireEvent.contextMenu(eventOf(container, "erp-145"));
      fireEvent.click(
        screen.getByRole("menuitem", {
          name: new RegExp(copy.menu.changeDates),
        }),
      );
      const dialog = screen.getByRole("dialog");
      const [start, due] = within(dialog).getAllByDisplayValue(
        /2026-10-1[46]/,
      ) as HTMLInputElement[];
      fireEvent.change(due!, { target: { value: "2026-10-12" } });
      expect(within(dialog).getByRole("alert").textContent).toContain(
        copy.dates.invalid,
      );
      expect(due!.getAttribute("aria-invalid")).toBe("true");
      const save = within(dialog).getByRole("button", {
        name: copy.dates.save,
      }) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
      fireEvent.change(due!, { target: { value: "2026-10-18" } });
      expect(start!.value).toBe("2026-10-14");
      expect(save.disabled).toBe(false);
      fireEvent.click(save);
      expect(changed).toEqual([
        ["erp-145", { startDay: "2026-10-14", dueDay: "2026-10-18" }],
      ]);
      unmount();
    }
  });

  it("FUI[wm.calendar.board:menu] moves an item back to Chưa lên lịch from the menu", () => {
    const unscheduled: string[] = [];
    const { container } = renderCalendar(
      withSpan(),
      {},
      {
        onChangeDates: () => undefined,
        onUnschedule: (entry) => unscheduled.push(entry.id),
      },
    );
    fireEvent.contextMenu(eventOf(container, "erp-145"));
    fireEvent.click(
      screen.getByRole("menuitem", { name: /Chuyển về Chưa lên lịch/ }),
    );
    expect(unscheduled).toEqual(["erp-145"]);
  });

  it("FUI[wm.calendar.board:undated-date] sets one day for an undated item by button (Đặt ngày)", () => {
    const set: Array<[string, string]> = [];
    renderCalendar(calendarScenario(), {
      onSetDue: (id, day) => set.push([id, day]),
    });
    fireEvent.click(
      screen.getByRole("button", { name: "Đặt ngày cho ERP-153" }),
    );
    expect(set).toEqual([["erp-153", "2026-10-12"]]);
  });

  it("FUI[wm.calendar.board:read-only-handles] keeps the handles disabled with the reason for a viewer", () => {
    const { container } = renderCalendar(
      { ...withSpan(calendarReadOnlyScenario()) },
      {},
      { onChangeDates: () => undefined },
    );
    const due = handle(container, "due");
    expect(due.getAttribute("aria-disabled")).toBe("true");
    expect(due.getAttribute("title")).toContain(
      "Bạn chỉ có quyền xem board này",
    );
    fireEvent.pointerDown(due, {
      clientX: 400,
      clientY: 50,
      button: 0,
      pointerId: 1,
    });
    expect(container.querySelector("[data-calendar-tip]")).toBeNull();
    // No menu for a viewer: the context menu has nothing to offer.
    fireEvent.contextMenu(eventOf(container, "erp-145"));
    expect(screen.queryByRole("menuitem")).toBeNull();
  });
});
