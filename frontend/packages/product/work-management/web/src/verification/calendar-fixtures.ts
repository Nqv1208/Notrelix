/**
 * Deterministic data for the Calendar surface: the ERP board's week of
 * Monday 12/10/2026 as `Final-Calendar` draws it (items, the three phases and
 * the Go-live milestone), plus its empty, edge and dense variants. The clock
 * is injected (12/10/2026 10:40, Hanoi); nothing here reads the system date.
 */
import { erpViewBar } from "./view-state-fixtures";
import type { BoardCalendarSurfaceProps } from "../components/views/calendar/board-calendar-surface";
import type { CalendarDragState } from "../components/views/calendar/calendar-grid";
import {
  calendarCopy,
  type CalendarCopy,
} from "../components/views/calendar/calendar.copy";
import type {
  CalendarEntry,
  CalendarPerson,
  CalendarRange,
  CalendarTone,
  CalendarUndated,
  DayKey,
} from "../components/views/calendar/calendar-model";
import { identityMarkClass } from "../components/shared/item-display";

export const CALENDAR_STORY_TODAY: DayKey = "2026-10-12";
/** 10:40. */
export const CALENDAR_STORY_NOW_MINUTES = 10 * 60 + 40;

export type CalendarMockupState =
  "binh-thuong" | "keo-tha" | "tao-nhanh" | "khong-ket-qua";

function person(initials: string, name: string): CalendarPerson {
  return { initials, name, markClass: identityMarkClass(name) };
}

const HA = person("HA", "Nguyễn Thu Hà");
const MQ = person("MQ", "Trần Minh Quân");
const HP = person("HP", "Lê Hoàng Phúc");
const NA = person("NA", "Phan Ngọc Anh");
const VT = person("VT", "Vũ Thanh");
const TT = person("TT", "Bùi Thanh Tâm");
const KL = person("KL", "Đỗ Khánh Linh");

const T142 = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

function item(
  key: string,
  title: string,
  tone: CalendarTone,
  startDay: DayKey,
  endDay: DayKey,
  assignee: CalendarPerson,
  note: string,
  dueMinutes: number | null = 17 * 60,
): CalendarEntry {
  return {
    id: key.toLowerCase(),
    kind: "item",
    itemKey: key,
    title,
    tone,
    startDay,
    endDay,
    dueMinutes,
    assignee,
    note,
    movable: true,
  };
}

function marker(
  id: string,
  kind: "phase" | "milestone",
  title: string,
  tone: CalendarTone,
  startDay: DayKey,
  endDay: DayKey,
): CalendarEntry {
  return {
    id,
    kind,
    title,
    tone,
    startDay,
    endDay,
    note: "",
    movable: false,
  };
}

/** Every entry of the mockup's October 2026 month. */
export function calendarMockupEntries(): CalendarEntry[] {
  return [
    marker(
      "phase-uat",
      "phase",
      "UAT phân hệ bán hàng với phòng Kinh doanh",
      "doing",
      "2026-10-12",
      "2026-10-16",
    ),
    marker(
      "phase-training",
      "phase",
      "Đào tạo phòng Kế toán",
      "todo",
      "2026-10-22",
      "2026-10-23",
    ),
    marker(
      "phase-parallel",
      "phase",
      "Chạy song song hệ thống cũ và ERP",
      "review",
      "2026-10-26",
      "2026-10-31",
    ),
    marker(
      "milestone-golive",
      "milestone",
      "Go-live ERP",
      "todo",
      "2026-11-01",
      "2026-11-01",
    ),
    item(
      "ERP-129",
      "Chốt danh mục tài khoản kế toán dùng chung",
      "done",
      "2026-09-30",
      "2026-09-30",
      KL,
      "hoàn thành 30/09",
    ),
    item(
      "ERP-142",
      T142,
      "overdue",
      "2026-10-01",
      "2026-10-10",
      MQ,
      "bắt đầu 01/10, hạn 10/10, quá hạn 2 ngày",
    ),
    item(
      "ERP-131",
      "Sao lưu cơ sở dữ liệu tự động hằng đêm",
      "done",
      "2026-10-05",
      "2026-10-05",
      VT,
      "hoàn thành",
    ),
    item(
      "ERP-144",
      "Kiểm thử phân hệ công nợ với số liệu tháng 9",
      "doing",
      "2026-10-12",
      "2026-10-12",
      HP,
      "hạn hôm nay",
    ),
    item(
      "ERP-138",
      "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
      "review",
      "2026-10-14",
      "2026-10-14",
      MQ,
      "chờ duyệt, hạn 14/10",
    ),
    item(
      "ERP-140",
      "Mẫu báo cáo doanh thu theo chi nhánh",
      "review",
      "2026-10-15",
      "2026-10-15",
      NA,
      "chờ duyệt, hạn 15/10",
    ),
    item(
      "ERP-146",
      "Đồng bộ tồn kho giữa kho Bình Dương và kho Long An",
      "doing",
      "2026-10-16",
      "2026-10-16",
      VT,
      "hạn 16/10",
    ),
    item(
      "ERP-151",
      "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
      "todo",
      "2026-10-18",
      "2026-10-18",
      NA,
      "hạn 18/10",
    ),
    item(
      "ERP-147",
      "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
      "todo",
      "2026-10-21",
      "2026-10-21",
      TT,
      "hạn 21/10",
    ),
    item(
      "ERP-149",
      "Viết tài liệu quy trình nhập hoá đơn đầu vào",
      "todo",
      "2026-10-25",
      "2026-10-25",
      KL,
      "hạn 25/10",
    ),
    item(
      "ERP-150",
      "Thiết lập cảnh báo khi đồng bộ tồn kho thất bại",
      "todo",
      "2026-10-28",
      "2026-10-28",
      VT,
      "hạn 28/10",
    ),
    item(
      "ERP-152",
      "Kế hoạch hỗ trợ người dùng tuần đầu vận hành",
      "todo",
      "2026-10-30",
      "2026-10-30",
      HA,
      "hạn 30/10",
    ),
  ];
}

export const CALENDAR_UNDATED: readonly CalendarUndated[] = [
  {
    id: "erp-153",
    itemKey: "ERP-153",
    title: "Rà soát hợp đồng bản quyền phần mềm với nhà cung cấp",
    assigneeName: null,
  },
];

export type CalendarScenario = Omit<
  BoardCalendarSurfaceProps,
  | "onRangeChange"
  | "onAnchorChange"
  | "onDateBasisChange"
  | "onClearFilter"
  | "onOpenItem"
  | "onReschedule"
  | "onSetDue"
  | "onCreateItem"
>;

export const CALENDAR_DRAG_PREVIEW: CalendarDragState = {
  source: "entry",
  id: "erp-146",
  text: "ERP-146 Đồng bộ tồn kho giữa kho Bình Dương và kho Long An",
  assignee: VT,
  fromDay: "2026-10-16",
  overDay: "2026-10-20",
};

/**
 * The surface for one mockup state and range. `keo-tha` is the drag preview
 * (month only), `tao-nhanh` the quick-create popover on 22/10 (month only),
 * `khong-ket-qua` a filter that hides every item.
 */
export function calendarScenario({
  state = "binh-thuong",
  range = "month",
  locale = "vi",
}: {
  state?: CalendarMockupState;
  range?: CalendarRange;
  locale?: "vi" | "en";
} = {}): CalendarScenario {
  const copy: CalendarCopy = calendarCopy(locale);
  const empty = state === "khong-ket-qua";
  const month = range === "month";
  return {
    copy,
    locale,
    range,
    anchorDay: CALENDAR_STORY_TODAY,
    today: CALENDAR_STORY_TODAY,
    nowMinutes: CALENDAR_STORY_NOW_MINUTES,
    zoneLabel: copy.zone.label
      .replace("{{zone}}", copy.zone.hanoi)
      .replace("{{offset}}", "GMT+7"),
    zoneName: copy.zone.hanoi,
    dateBasis: "span",
    entries: empty ? [] : calendarMockupEntries(),
    undatedTotal: CALENDAR_UNDATED.length,
    undated: empty ? [] : CALENDAR_UNDATED,
    viewBar: erpViewBar({
      locale,
      view: "calendar",
      ...(empty
        ? {
            state: {
              filter: {
                join: "and" as const,
                conditions: [
                  {
                    fieldId: "field-erp-owner",
                    operator: "has_any" as const,
                    values: ["user-khanh-linh"],
                  },
                ],
              },
            },
            counts: { shown: 0, total: 16 },
          }
        : {}),
    }),
    filterActive: empty,
    hiddenCount: empty ? 16 : 0,
    canEdit: true,
    defaultDrag: state === "keo-tha" && month ? CALENDAR_DRAG_PREVIEW : null,
    defaultQuickCreateDay: state === "tao-nhanh" && month ? "2026-10-22" : null,
  };
}

/** A viewer: the same board without add, drag, quick create or "Đặt hạn". */
export function calendarReadOnlyScenario(): CalendarScenario {
  return { ...calendarScenario(), canEdit: false };
}

/** A board with no item at all. */
export function calendarEmptyScenario(): CalendarScenario {
  return {
    ...calendarScenario(),
    entries: [],
    undated: [],
    undatedTotal: 0,
  };
}

/**
 * Edge data: a very long title, an item without key or assignee, a phase
 * that crosses the month end and an item that sits on a day outside the
 * month.
 */
export function calendarEdgeScenario(): CalendarScenario {
  const base = calendarScenario();
  return {
    ...base,
    entries: [
      ...base.entries,
      item(
        "ERP-160",
        "Đối soát toàn bộ số dư công nợ phải thu và phải trả của mười hai chi nhánh trước khi khoá sổ kế toán quý IV",
        "doing",
        "2026-10-13",
        "2026-10-13",
        HP,
        "hạn 13/10",
      ),
      {
        id: "bare",
        kind: "item",
        title: "Việc chưa có mã hay người phụ trách",
        tone: "todo",
        startDay: "2026-10-20",
        endDay: "2026-10-20",
        dueMinutes: 17 * 60,
        assignee: null,
        note: "hạn 20/10",
        movable: true,
      },
      item(
        "ERP-161",
        "Chốt nội dung nghiệm thu giai đoạn một",
        "todo",
        "2026-10-29",
        "2026-11-03",
        TT,
        "bắt đầu 29/10, hạn 03/11",
      ),
    ],
  };
}

/** High density: seven items due on 14/10 and 20/10, so "+N" appears. */
export function calendarDenseScenario(): CalendarScenario {
  const base = calendarScenario();
  const crowd = (day: DayKey, count: number) =>
    Array.from({ length: count }, (_, index) =>
      item(
        `ERP-2${index}${day.slice(-2)}`,
        `Việc dồn trong ngày số ${index + 1}`,
        index % 2 === 0 ? "doing" : "todo",
        day,
        day,
        index % 2 === 0 ? HP : NA,
        `hạn ${day.slice(-2)}/10`,
      ),
    );
  return {
    ...base,
    entries: [
      ...base.entries,
      ...crowd("2026-10-14", 5),
      ...crowd("2026-10-20", 4),
    ],
  };
}
