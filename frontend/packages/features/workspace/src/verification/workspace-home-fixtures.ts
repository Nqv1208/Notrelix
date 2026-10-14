/**
 * Deterministic fixtures and scenarios for Home, My work and onboarding
 * (WX-HOME-001/002). Fixed reference time at noon UTC so local-day grouping
 * is the same in every timezone between UTC−11 and UTC+11. No randomness,
 * no network.
 */
import { createFormatters } from "@notrelix/i18n";
import {
  groupMyWorkItems,
  type MyWorkItem,
} from "../core/model/my-work";
import { NAVIGATION_RESOURCE_KIND } from "../core/model/navigation";
import type {
  ContinueEntry,
  MyWorkGroupView,
  MyWorkItemView,
  SurfaceFormatters,
} from "../web/home/home-models";
import {
  workspaceHomeCopy,
  type WorkspaceHomeCopy,
} from "../web/home/workspace-home.copy";
import type { WorkspaceHomeSurfaceProps } from "../web/home/workspace-home-surface";
import type { WorkspaceMyWorkSurfaceProps } from "../web/home/workspace-my-work-surface";
import { shellNavigationFixture } from "./workspace-shell-fixtures";

/** 2026-10-12T12:00:00Z. */
export const HOME_FIXTURE_NOW = Date.UTC(2026, 9, 12, 12, 0, 0);

const DAY = 24 * 60 * 60 * 1000;
const HOUR = 60 * 60 * 1000;

function at(offsetMs: number): string {
  return new Date(HOME_FIXTURE_NOW + offsetMs).toISOString();
}

export function homeCopyFixture(locale: "en" | "vi" = "en"): WorkspaceHomeCopy {
  return workspaceHomeCopy(locale);
}

export function homeFormattersFixture(
  locale: "en" | "vi" = "en",
): SurfaceFormatters {
  return createFormatters(locale);
}

export const HOME_BOARD_NAMES: Readonly<Record<string, string>> = {
  "board-roadmap": "Product Roadmap",
  "board-launch": "Q4 Launch",
  "board-triage": "Bug Triage",
};

export function continueEntry(
  overrides: Partial<ContinueEntry> &
    Pick<ContinueEntry, "resourceId" | "name">,
): ContinueEntry {
  return {
    kind: NAVIGATION_RESOURCE_KIND.board,
    at: at(-2 * HOUR),
    pinned: false,
    location: null,
    ...overrides,
  };
}

export function continueEntriesFixture(): ContinueEntry[] {
  return [
    continueEntry({
      resourceId: "board-roadmap",
      name: "Product Roadmap",
      at: at(-25 * 60 * 1000),
      pinned: true,
    }),
    continueEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-retro",
      name: "Sprint 42 retro",
      location: "Team rituals",
      at: at(-3 * HOUR),
    }),
    continueEntry({
      resourceId: "board-launch",
      name: "Q4 Launch",
      at: at(-20 * HOUR),
    }),
    continueEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-onboarding",
      name: "Onboarding guide",
      location: "Product",
      at: at(-2 * DAY),
    }),
    continueEntry({
      resourceId: "board-triage",
      name: "Bug Triage",
      at: at(-4 * DAY),
    }),
    continueEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-pricing",
      name: "Pricing research notes",
      location: "Product",
      at: at(-9 * DAY),
    }),
  ];
}

export function myWorkItem(
  overrides: Partial<MyWorkItem> & Pick<MyWorkItem, "id" | "name">,
): MyWorkItem {
  return {
    boardId: "board-roadmap",
    groupId: "group-now",
    dueAt: null,
    startedAt: null,
    updatedAt: at(-DAY),
    version: 3,
    ...overrides,
  };
}

export function myWorkItemsFixture(): MyWorkItem[] {
  return [
    myWorkItem({
      id: "item-contract",
      name: "Review vendor contract",
      boardId: "board-launch",
      dueAt: at(-2 * DAY),
    }),
    myWorkItem({
      id: "item-qa",
      name: "Sign off QA checklist",
      dueAt: at(-DAY),
    }),
    myWorkItem({
      id: "item-copy",
      name: "Final pass on launch copy",
      boardId: "board-launch",
      dueAt: at(0),
    }),
    myWorkItem({
      id: "item-standup",
      name: "Prepare roadmap update for Friday review",
      dueAt: at(2 * HOUR),
    }),
    myWorkItem({
      id: "item-pricing",
      name: "Pricing page experiment",
      dueAt: at(2 * DAY),
    }),
    myWorkItem({
      id: "item-triage",
      name: "Triage new crash reports",
      boardId: "board-triage",
      dueAt: at(4 * DAY),
    }),
    myWorkItem({
      id: "item-okrs",
      name: "Draft Q1 objectives",
      dueAt: at(20 * DAY),
    }),
    myWorkItem({
      id: "item-cleanup",
      name: "Archive stale automations",
      boardId: "board-triage",
      dueAt: null,
    }),
  ];
}

export function withBoardNames(
  groups: ReturnType<typeof groupMyWorkItems>,
  boardNames: Readonly<Record<string, string>> = HOME_BOARD_NAMES,
): MyWorkGroupView[] {
  return groups.map((group) => ({
    range: group.range,
    items: group.items.map((item): MyWorkItemView => ({
      ...item,
      boardName: boardNames[item.boardId] ?? null,
    })),
  }));
}

export function myWorkGroupsFixture(): MyWorkGroupView[] {
  return withBoardNames(
    groupMyWorkItems(myWorkItemsFixture(), HOME_FIXTURE_NOW),
  );
}

/**
 * Final-Home's own data at its clock (Monday 12 October 2026, 10:40 in the
 * member's calendar), so a story can be measured against the mockup. Built
 * from local calendar parts so the story reads the same in every timezone.
 */
export const HOME_MOCKUP_NOW = new Date(2026, 9, 12, 10, 40).getTime();

function mockupAt(day: number, hour = 0, minute = 0): string {
  return new Date(2026, 9, day, hour, minute).toISOString();
}

const MOCKUP_ERP = "Triển khai ERP Q4";
const MOCKUP_BOARD_IDS = { erp: "board-erp", kt: "board-kt", mk: "board-mk" };
const MOCKUP_DEPARTMENTS = {
  cn: { name: "Công nghệ", markTone: "blue" },
  kt: { name: "Kế toán", markTone: "gold" },
  mk: { name: "Marketing", markTone: "coral" },
} as const;

function mockupItem(
  overrides: Partial<MyWorkItemView> &
    Pick<MyWorkItemView, "id" | "name" | "boardName" | "dueAt">,
): MyWorkItemView {
  return {
    boardId: MOCKUP_BOARD_IDS.erp,
    groupId: "group-now",
    startedAt: null,
    updatedAt: mockupAt(11, 9, 0),
    version: 3,
    ...overrides,
  };
}

/** The seven open items of Final-Home / Final-MyWork `tat-ca`. */
export function homeMockupItems(): MyWorkItemView[] {
  return [
    mockupItem({
      id: "item-erp-142",
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      boardName: MOCKUP_ERP,
      department: MOCKUP_DEPARTMENTS.cn,
      key: "ERP-142",
      reason: "follow",
      priority: "urgent",
      status: { bucket: "doing", label: "Đang làm" },
      dueAt: mockupAt(10),
    }),
    mockupItem({
      id: "item-erp-144",
      name: "Kiểm thử phân hệ công nợ với số liệu tháng 9",
      boardName: MOCKUP_ERP,
      department: MOCKUP_DEPARTMENTS.cn,
      key: "ERP-144",
      reason: "follow",
      priority: "high",
      status: { bucket: "doing", label: "Đang làm" },
      dueAt: mockupAt(12),
    }),
    mockupItem({
      id: "item-kt-88",
      name: "Đối chiếu sao kê ngân hàng tháng 9",
      boardId: MOCKUP_BOARD_IDS.kt,
      boardName: "Đóng sổ tháng 10",
      department: MOCKUP_DEPARTMENTS.kt,
      key: "KT-88",
      reason: "mine",
      priority: "medium",
      status: { bucket: "todo", label: "Cần làm" },
      dueAt: mockupAt(12, 17, 0),
    }),
    mockupItem({
      id: "item-erp-138",
      name: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
      boardName: MOCKUP_ERP,
      department: MOCKUP_DEPARTMENTS.cn,
      key: "ERP-138",
      reason: "approve",
      priority: "high",
      status: { bucket: "review", label: "Chờ duyệt" },
      dueAt: mockupAt(14),
    }),
    mockupItem({
      id: "item-mk-21",
      name: "Góp ý nội dung trang giới thiệu sản phẩm mới",
      boardId: MOCKUP_BOARD_IDS.mk,
      boardName: "Ra mắt sản phẩm Q4",
      department: MOCKUP_DEPARTMENTS.mk,
      key: "MK-21",
      reason: "mine",
      priority: "medium",
      status: { bucket: "todo", label: "Cần làm" },
      dueAt: mockupAt(16),
    }),
    mockupItem({
      id: "item-kt-95",
      name: "Xác nhận số dư tài khoản ngân hàng trên ERP tháng 10",
      boardId: MOCKUP_BOARD_IDS.kt,
      boardName: "Đóng sổ tháng 10",
      department: MOCKUP_DEPARTMENTS.kt,
      key: "KT-95",
      reason: "mine",
      priority: "medium",
      status: { bucket: "todo", label: "Cần làm" },
      dueAt: mockupAt(22),
    }),
    mockupItem({
      id: "item-erp-152",
      name: "Kế hoạch hỗ trợ người dùng tuần đầu vận hành",
      boardName: MOCKUP_ERP,
      department: MOCKUP_DEPARTMENTS.cn,
      key: "ERP-152",
      reason: "mine",
      priority: "low",
      status: { bucket: "todo", label: "Cần làm" },
      dueAt: mockupAt(30),
    }),
  ];
}

function mockupGroups(items: readonly MyWorkItemView[]): MyWorkGroupView[] {
  return groupMyWorkItems(items, HOME_MOCKUP_NOW).map((group) => ({
    range: group.range,
    items: group.items as MyWorkItemView[],
  }));
}

function mockupContinue(): ContinueEntry[] {
  return [
    continueEntry({
      resourceId: MOCKUP_BOARD_IDS.erp,
      name: MOCKUP_ERP,
      location: "Công nghệ",
      at: new Date(HOME_MOCKUP_NOW - 20 * 60 * 1000).toISOString(),
    }),
    continueEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-invoice",
      name: "Quy trình nhập hoá đơn đầu vào",
      location: "Kế toán",
      at: mockupAt(11, 10, 40),
    }),
  ];
}

type HomeScenario = Omit<
  WorkspaceHomeSurfaceProps,
  | "onComplete"
  | "onAddItem"
  | "onCreateBoard"
  | "onCreateDoc"
  | "onInvite"
  | "onRetryContinue"
  | "onRetryMyWork"
>;

/** Final-Home `binh-thuong`: every card, as the mockup draws it. */
export function homeDefaultScenario(locale: "en" | "vi" = "vi"): HomeScenario {
  return {
    copy: homeCopyFixture(locale),
    navigation: shellNavigationFixture(),
    formatters: homeFormattersFixture(locale),
    now: HOME_MOCKUP_NOW,
    workspaceName: "Minh An Group",
    greetingName: "Hà",
    continueState: { status: "ready", data: mockupContinue() },
    myWork: { status: "ready", data: mockupGroups(homeMockupItems()) },
    myWorkHasMore: false,
    completingIds: new Set(),
    approvalCount: 1,
    mentionCount: 1,
    meetingCount: 3,
    replies: {
      status: "ready",
      data: [
        {
          id: "reply-sso",
          kind: "approve",
          actorName: "Trần Minh Quân",
          actorTone: "blue",
          when: "12 phút trước",
          text: "Gửi duyệt việc thiết lập đăng nhập một lần (SSO) cho toàn công ty (ERP-138). Kế toán cần đăng nhập thử từ thứ Năm.",
          target: {
            section: "board",
            boardId: "board-erp",
            item: "item-erp-138",
          },
        },
        {
          id: "reply-invoice",
          kind: "mention",
          actorName: "Lý Thu Trang",
          actorTone: "coral",
          when: "1 giờ trước",
          text: "@Hà chị xem giúp đề xuất 3 ngày đối chiếu cuối tháng trong quy trình nhập hoá đơn.",
          target: { section: "doc", docId: "doc-invoice" },
        },
      ],
    },
    agenda: {
      status: "ready",
      data: {
        nowTime: "10:40",
        entries: [
          {
            id: "ev-standup",
            time: "09:00",
            title: "Họp đứng nhóm ERP",
            detail: "15 phút · Phòng họp 3",
            past: true,
          },
          {
            id: "ev-uat",
            time: "14:00",
            title: "Demo UAT phân hệ bán hàng",
            detail: "1 giờ · trực tuyến · 6 người",
            past: false,
          },
          {
            id: "ev-1on1",
            time: "16:30",
            title: "Họp 1-1 với Phan Ngọc Anh",
            detail: "30 phút",
            past: false,
          },
        ],
      },
    },
    calendarHref: "#calendar",
    onboarding: null,
  };
}

/** Final-Home `xong-viec`: nothing due today, one overdue. */
export function homeAllDoneScenario(): HomeScenario {
  const items = homeMockupItems().filter(
    (item) => item.id !== "item-erp-144" && item.id !== "item-kt-88",
  );
  return {
    ...homeDefaultScenario(),
    myWork: { status: "ready", data: mockupGroups(items) },
  };
}

/** Final-Home `dang-tai`. */
export function homeLoadingScenario(): HomeScenario {
  return {
    ...homeDefaultScenario(),
    workspaceName: null,
    continueState: { status: "loading" },
    myWork: { status: "loading" },
    replies: { status: "loading" },
    agenda: { status: "loading" },
    approvalCount: undefined,
    mentionCount: undefined,
    meetingCount: undefined,
  };
}

/** What the real host can fill today: no replies, agenda or relationship. */
export function homeProducedOnlyScenario(): HomeScenario {
  const items = homeMockupItems().map((item): MyWorkItemView => ({
    ...item,
    key: null,
    reason: null,
    priority: null,
    status: null,
  }));
  return {
    ...homeDefaultScenario(),
    myWork: { status: "ready", data: mockupGroups(items) },
    replies: undefined,
    agenda: undefined,
    approvalCount: undefined,
    mentionCount: undefined,
    meetingCount: undefined,
  };
}

export function homeErrorScenario(): HomeScenario {
  return {
    ...homeDefaultScenario(),
    continueState: { status: "error" },
    myWork: { status: "error" },
    replies: undefined,
    agenda: undefined,
  };
}

/** Long names, a Board the member cannot open, no department, partial data. */
export function homeEdgeDataScenario(): HomeScenario {
  const long = mockupItem({
    id: "item-long",
    name: "Chuẩn bị tài liệu bàn giao cho đội vận hành khu vực, bao gồm sổ tay xử lý sự cố và danh sách liên hệ khẩn cấp",
    boardId: "board-unknown",
    boardName: null,
    dueAt: mockupAt(9),
  });
  return {
    ...homeDefaultScenario(),
    workspaceName:
      "Phòng Kế hoạch & Vận hành Khu vực Đông Nam Á — Năm tài chính 2026",
    greetingName: "Nguyễn Thị Phương Thảo Nguyên",
    continueState: {
      status: "ready",
      data: [
        continueEntry({
          resourceId: "board-long",
          name: "Lộ trình tích hợp hệ thống thanh toán đa quốc gia cho thị trường Đông Nam Á giai đoạn hai",
          at: new Date(HOME_MOCKUP_NOW - 30 * 1000).toISOString(),
        }),
      ],
    },
    myWork: { status: "ready", data: mockupGroups([long]) },
    replies: undefined,
    agenda: undefined,
    approvalCount: undefined,
    mentionCount: undefined,
  };
}

/** A new workspace (WX-HOME-002): onboarding replaces the work sections. */
export function homeOnboardingScenario(
  overrides: Partial<NonNullable<HomeScenario["onboarding"]>> = {},
): HomeScenario {
  return {
    ...homeDefaultScenario(),
    continueState: { status: "ready", data: [] },
    myWork: { status: "ready", data: [] },
    replies: undefined,
    agenda: undefined,
    approvalCount: undefined,
    mentionCount: undefined,
    onboarding: { canCreate: true, canInvite: true, ...overrides },
  };
}

type MyWorkScenario = Omit<
  WorkspaceMyWorkSurfaceProps,
  "onComplete" | "onRetry" | "onLoadMore"
>;

export function myWorkDefaultScenario(): MyWorkScenario {
  return {
    copy: homeCopyFixture(),
    navigation: shellNavigationFixture(),
    formatters: homeFormattersFixture(),
    state: { status: "ready", data: myWorkGroupsFixture() },
    completingIds: new Set(),
    hasMore: true,
    loadingMore: false,
  };
}

export function myWorkLoadingScenario(): MyWorkScenario {
  return {
    ...myWorkDefaultScenario(),
    state: { status: "loading" },
    hasMore: false,
  };
}

export function myWorkEmptyScenario(): MyWorkScenario {
  return {
    ...myWorkDefaultScenario(),
    state: { status: "ready", data: [] },
    hasMore: false,
  };
}

export function myWorkErrorScenario(): MyWorkScenario {
  return {
    ...myWorkDefaultScenario(),
    state: { status: "error" },
    hasMore: false,
  };
}

/** 120 items across windows, one completing, a page still loading. */
export function myWorkHighDensityScenario(): MyWorkScenario {
  const items: MyWorkItem[] = Array.from({ length: 120 }, (_, index) =>
    myWorkItem({
      id: `item-dense-${index}`,
      name: `Item ${String(index + 1).padStart(3, "0")} — follow up`,
      boardId: ["board-roadmap", "board-launch", "board-triage"][index % 3]!,
      dueAt:
        index % 9 === 0
          ? null
          : new Date(HOME_FIXTURE_NOW + (index - 20) * 6 * HOUR).toISOString(),
    }),
  );
  return {
    ...myWorkDefaultScenario(),
    state: {
      status: "ready",
      data: withBoardNames(groupMyWorkItems(items, HOME_FIXTURE_NOW)),
    },
    completingIds: new Set(["item-dense-3"]),
    loadingMore: true,
  };
}

export function myWorkEdgeDataScenario(): MyWorkScenario {
  const home = homeEdgeDataScenario();
  return {
    ...myWorkDefaultScenario(),
    copy: homeCopyFixture("vi"),
    formatters: homeFormattersFixture("vi"),
    state: home.myWork,
    hasMore: false,
  };
}
