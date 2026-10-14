/**
 * Deterministic data for the Timeline surface: the ERP board of
 * `Final-Timeline` (four groups, twelve items, the three phases, the Go-live
 * milestone and the seven finish-to-start links), the drag state `keo-gian`
 * and the critical path state `duong-gang`, plus its empty, edge, dense and
 * read-only variants. The clock is injected (12/10/2026, Hanoi); nothing here
 * reads the system date.
 */
import { identityMarkClass } from "../components/shared/item-display";
import type { BoardTimelineSurfaceProps } from "../components/views/timeline/board-timeline-surface";
import type { CalendarPerson } from "../components/views/calendar/calendar-model";
import type {
  TimelineDrag,
  TimelineGroup,
  TimelineItem,
  TimelineLink,
  TimelineScale,
  TimelineTone,
} from "../components/views/timeline/timeline-model";
import {
  timelineCopy,
  type TimelineCopy,
} from "../components/views/timeline/timeline.copy";

export const TIMELINE_STORY_TODAY = "2026-10-12";

export type TimelineMockupState = "binh-thuong" | "keo-gian" | "duong-gang";

function person(initials: string, name: string): CalendarPerson {
  return { initials, name, markClass: identityMarkClass(name) };
}

const MQ = person("MQ", "Trần Minh Quân");
const HP = person("HP", "Lê Hoàng Phúc");
const NA = person("NA", "Phan Ngọc Anh");
const VT = person("VT", "Vũ Thanh");
const TT = person("TT", "Bùi Thanh Tâm");
const KL = person("KL", "Đỗ Khánh Linh");

/** "dd/MM" of the mockup (2026) as a day key. */
function day(dm: string): string {
  const [d = "01", m = "01"] = dm.split("/");
  return `2026-${m}-${d}`;
}

function task(
  key: string,
  title: string,
  tone: TimelineTone,
  start: string | null,
  due: string,
  assignee: CalendarPerson,
  percent: number,
  critical = false,
): TimelineItem {
  return {
    id: key.toLowerCase(),
    kind: "item",
    itemKey: key,
    title,
    tone,
    startDay: start ? day(start) : null,
    dueDay: day(due),
    percent,
    assignee,
    critical,
    movable: true,
    href: `#${key.toLowerCase()}`,
  };
}

function marker(
  id: string,
  kind: "phase" | "milestone",
  title: string,
  tone: TimelineTone,
  start: string,
  due: string,
  percent: number,
  critical = false,
): TimelineItem {
  return {
    id,
    kind,
    title,
    tone,
    startDay: day(start),
    dueDay: day(due),
    percent,
    critical,
    movable: false,
    href: `#${id}`,
  };
}

/** The mockup's rows, group by group. */
export function timelineMockupGroups(): TimelineGroup[] {
  return [
    {
      id: "g-prepare",
      title: "Chuẩn bị",
      items: [
        task(
          "ERP-129",
          "Chốt danh mục tài khoản kế toán dùng chung",
          "done",
          null,
          "30/09",
          KL,
          100,
          true,
        ),
        task(
          "ERP-151",
          "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
          "todo",
          "13/10",
          "18/10",
          NA,
          0,
        ),
      ],
    },
    {
      id: "g-data",
      title: "Dữ liệu",
      items: [
        task(
          "ERP-142",
          "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
          "doing",
          "01/10",
          "10/10",
          MQ,
          60,
          true,
        ),
        task(
          "ERP-146",
          "Đồng bộ tồn kho giữa kho Bình Dương và kho Long An",
          "doing",
          "13/10",
          "16/10",
          VT,
          33,
        ),
        task(
          "ERP-150",
          "Thiết lập cảnh báo khi đồng bộ tồn kho thất bại",
          "todo",
          "22/10",
          "28/10",
          VT,
          0,
        ),
      ],
    },
    {
      id: "g-test",
      title: "Kiểm thử",
      items: [
        task(
          "ERP-144",
          "Kiểm thử phân hệ công nợ với số liệu tháng 9",
          "doing",
          "08/10",
          "12/10",
          HP,
          67,
        ),
        marker(
          "phase-uat",
          "phase",
          "UAT phân hệ bán hàng với phòng Kinh doanh",
          "doing",
          "12/10",
          "16/10",
          10,
        ),
        task(
          "ERP-138",
          "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
          "review",
          "09/10",
          "14/10",
          MQ,
          90,
        ),
      ],
    },
    {
      id: "g-train",
      title: "Đào tạo và vận hành",
      items: [
        task(
          "ERP-147",
          "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
          "todo",
          "16/10",
          "21/10",
          TT,
          0,
          true,
        ),
        marker(
          "phase-training",
          "phase",
          "Đào tạo phòng Kế toán",
          "todo",
          "22/10",
          "23/10",
          0,
          true,
        ),
        marker(
          "phase-parallel",
          "phase",
          "Chạy song song hệ thống cũ và ERP",
          "todo",
          "26/10",
          "31/10",
          0,
          true,
        ),
        marker(
          "milestone-golive",
          "milestone",
          "Go-live ERP",
          "todo",
          "01/11",
          "01/11",
          0,
          true,
        ),
      ],
    },
  ];
}

/** The mockup's seven finish-to-start links. */
export function timelineMockupLinks(): TimelineLink[] {
  const link = (from: string, to: string) => ({ from, to });
  return [
    link("erp-129", "erp-142"),
    link("erp-142", "erp-146"),
    link("erp-142", "erp-147"),
    link("erp-146", "erp-150"),
    link("erp-147", "phase-training"),
    link("phase-training", "phase-parallel"),
    link("phase-parallel", "milestone-golive"),
  ];
}

/** Final-Timeline keo-gian: ERP-147's due day pulled from 21/10 to 23/10. */
export const TIMELINE_DRAG_PREVIEW: TimelineDrag = {
  id: "erp-147",
  mode: "resize",
  days: 2,
};

export type TimelineScenario = Omit<
  BoardTimelineSurfaceProps,
  | "onScaleChange"
  | "onAnchorChange"
  | "onOpenItem"
  | "onReschedule"
  | "onAddItem"
  | "onOpenUndated"
>;

export function timelineScenario({
  state = "binh-thuong",
  scale = "day",
  locale = "vi",
}: {
  state?: TimelineMockupState;
  scale?: TimelineScale;
  locale?: "vi" | "en";
} = {}): TimelineScenario {
  const copy: TimelineCopy = timelineCopy(locale);
  return {
    copy,
    locale,
    boardTitle: "Triển khai ERP Q4",
    scale,
    anchorDay: TIMELINE_STORY_TODAY,
    today: TIMELINE_STORY_TODAY,
    zoneName: copy.zone.hanoi,
    groups: timelineMockupGroups(),
    links: timelineMockupLinks(),
    undatedCount: 1,
    undatedHref: "#undated",
    canEdit: true,
    defaultDrag: state === "keo-gian" ? TIMELINE_DRAG_PREVIEW : null,
    defaultCriticalPath: state === "duong-gang",
  };
}

/** A viewer: the same board without drag, keyboard move or add. */
export function timelineReadOnlyScenario(): TimelineScenario {
  return { ...timelineScenario(), canEdit: false };
}

/** A board with no dated item. */
export function timelineEmptyScenario(): TimelineScenario {
  return { ...timelineScenario(), groups: [], links: [], undatedCount: 0 };
}

/**
 * Edge data: a very long title, an item without key or assignee, a bar that
 * starts before the window and one that ends after it, a one-day bar, an
 * item due before the window and one that starts after it.
 */
export function timelineEdgeScenario(): TimelineScenario {
  const base = timelineScenario();
  const extra: TimelineGroup = {
    id: "g-edge",
    title: "Giới hạn",
    items: [
      task(
        "ERP-160",
        "Đối soát toàn bộ số dư công nợ phải thu và phải trả của mười hai chi nhánh trước khi khoá sổ kế toán quý IV",
        "doing",
        "14/10",
        "23/10",
        HP,
        40,
      ),
      {
        id: "bare",
        kind: "item",
        title: "Việc chưa có mã hay người phụ trách",
        tone: "todo",
        startDay: day("20/10"),
        dueDay: day("20/10"),
        percent: 0,
        assignee: null,
        movable: true,
      },
      task(
        "ERP-161",
        "Bắt đầu trước khoảng đang xem",
        "doing",
        "28/09",
        "08/10",
        TT,
        80,
      ),
      task(
        "ERP-162",
        "Kết thúc sau khoảng đang xem",
        "todo",
        "30/10",
        "20/11",
        NA,
        0,
      ),
      task(
        "ERP-163",
        "Hạn trước khoảng đang xem",
        "todo",
        "01/09",
        "10/09",
        KL,
        0,
      ),
      task(
        "ERP-164",
        "Bắt đầu sau khoảng đang xem",
        "todo",
        "20/12",
        "28/12",
        MQ,
        0,
      ),
    ],
  };
  return { ...base, groups: [...base.groups, extra] };
}

/** High density: forty items over three groups. */
export function timelineDenseScenario(): TimelineScenario {
  const tones: TimelineTone[] = ["todo", "doing", "review", "done"];
  const groups: TimelineGroup[] = [0, 1, 2].map((groupIndex) => ({
    id: `dense-${groupIndex}`,
    title: `Nhóm ${groupIndex + 1}`,
    items: Array.from({ length: 14 }, (_, index) => {
      const start = 5 + ((index * 2 + groupIndex) % 20);
      const tone = tones[(index + groupIndex) % tones.length] ?? "todo";
      return task(
        `ERP-3${groupIndex}${String(index).padStart(2, "0")}`,
        `Việc dày đặc số ${index + 1} của nhóm ${groupIndex + 1}`,
        tone,
        `${String(start).padStart(2, "0")}/10`,
        `${String(Math.min(31, start + 2 + (index % 5))).padStart(2, "0")}/10`,
        index % 2 === 0 ? HP : NA,
        tone === "done" ? 100 : (index * 7) % 90,
      );
    }),
  }));
  return { ...timelineScenario(), groups, links: [], undatedCount: 0 };
}
