/**
 * Deterministic fixtures and scenarios for Home, My work and onboarding
 * (WX-HOME-001/002). Fixed reference time at noon UTC so local-day grouping
 * is the same in every timezone between UTC−11 and UTC+11. No randomness,
 * no network.
 */
import { createFormatters } from "@notrelix/i18n";
import {
  groupMyWorkItems,
  limitMyWorkGroups,
  type MyWorkItem,
} from "../core/model/my-work";
import { NAVIGATION_RESOURCE_KIND } from "../core/model/navigation";
import type {
  WorkspaceActivityItem,
  WorkspaceInvitation,
} from "../core/types/workspace";
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

export function activityEntry(
  overrides: Partial<WorkspaceActivityItem> &
    Pick<WorkspaceActivityItem, "id" | "actor" | "action" | "target">,
): WorkspaceActivityItem {
  return { createdAt: at(-HOUR), ...overrides };
}

export function activityFixture(): WorkspaceActivityItem[] {
  return [
    activityEntry({
      id: "act-1",
      actor: "Ada Lovelace",
      action: "edited",
      target: "Sprint 42 retro",
      createdAt: at(-40 * 60 * 1000),
    }),
    activityEntry({
      id: "act-2",
      actor: "Alan Turing",
      action: "created",
      target: "Bug Triage",
      createdAt: at(-5 * HOUR),
    }),
    activityEntry({
      id: "act-3",
      actor: "Grace Hopper",
      action: "joined",
      target: "Product",
      createdAt: at(-DAY),
    }),
  ];
}

export function homeInvitation(
  overrides: Partial<WorkspaceInvitation> &
    Pick<WorkspaceInvitation, "id" | "email">,
): WorkspaceInvitation {
  return {
    role: "member",
    expiresAt: at(5 * DAY),
    isAccepted: false,
    createdAt: at(-2 * DAY),
    ...overrides,
  };
}

export function homeInvitationsFixture(): WorkspaceInvitation[] {
  return [
    homeInvitation({ id: "inv-1", email: "linh.nguyen@example.com" }),
    homeInvitation({
      id: "inv-2",
      email: "sam.okafor@example.com",
      role: "admin",
      expiresAt: at(DAY),
    }),
  ];
}

type HomeScenario = Omit<
  WorkspaceHomeSurfaceProps,
  | "onComplete"
  | "onCreateBoard"
  | "onCreateDoc"
  | "onInvite"
  | "onRetryContinue"
  | "onRetryMyWork"
  | "onRetryActivity"
  | "onRetryInvitations"
>;

export function homeDefaultScenario(locale: "en" | "vi" = "en"): HomeScenario {
  return {
    copy: homeCopyFixture(locale),
    navigation: shellNavigationFixture(),
    formatters: homeFormattersFixture(locale),
    now: HOME_FIXTURE_NOW,
    workspaceName: "Product",
    continueState: { status: "ready", data: continueEntriesFixture() },
    myWork: {
      status: "ready",
      data: limitMyWorkGroups(myWorkGroupsFixture(), 6) as MyWorkGroupView[],
    },
    myWorkHasMore: true,
    completingIds: new Set(["item-qa"]),
    activity: { status: "ready", data: activityFixture() },
    invitations: { status: "ready", data: homeInvitationsFixture() },
    onboarding: null,
  };
}

export function homeLoadingScenario(): HomeScenario {
  return {
    ...homeDefaultScenario(),
    workspaceName: null,
    continueState: { status: "loading" },
    myWork: { status: "loading" },
    myWorkHasMore: false,
    completingIds: new Set(),
    activity: { status: "loading" },
    invitations: { status: "loading" },
  };
}

export function homeErrorScenario(): HomeScenario {
  return {
    ...homeDefaultScenario(),
    continueState: { status: "error" },
    myWork: { status: "error" },
    myWorkHasMore: false,
    activity: { status: "error" },
    invitations: { status: "error" },
  };
}

/** Partial data: some sections loaded, one failed, one empty, long names. */
export function homeEdgeDataScenario(): HomeScenario {
  return {
    ...homeDefaultScenario("vi"),
    workspaceName:
      "Phòng Kế hoạch & Vận hành Khu vực Đông Nam Á — Năm tài chính 2026",
    continueState: {
      status: "ready",
      data: [
        continueEntry({
          resourceId: "board-long",
          name: "Lộ trình tích hợp hệ thống thanh toán đa quốc gia cho thị trường Đông Nam Á giai đoạn hai",
          at: new Date(HOME_FIXTURE_NOW - 30 * 1000).toISOString(),
        }),
        continueEntry({
          kind: NAVIGATION_RESOURCE_KIND.doc,
          resourceId: "doc-long",
          name: "Biên bản họp: Đánh giá rủi ro vận hành và kế hoạch dự phòng",
          location: "Tài liệu nội bộ / Vận hành / Quý 4",
          at: new Date(HOME_FIXTURE_NOW - 400 * DAY).toISOString(),
        }),
      ],
    },
    myWork: {
      status: "ready",
      data: withBoardNames(
        groupMyWorkItems(
          [
            myWorkItem({
              id: "item-long",
              name: "Chuẩn bị tài liệu bàn giao cho đội vận hành khu vực, bao gồm sổ tay xử lý sự cố và danh sách liên hệ khẩn cấp",
              boardId: "board-unknown",
              dueAt: new Date(HOME_FIXTURE_NOW - 3 * DAY).toISOString(),
            }),
          ],
          HOME_FIXTURE_NOW,
        ),
      ),
    },
    myWorkHasMore: false,
    completingIds: new Set(),
    activity: { status: "ready", data: [] },
    invitations: { status: "error" },
  };
}

/** A new workspace (WX-HOME-002): onboarding replaces Continue. */
export function homeOnboardingScenario(
  overrides: Partial<NonNullable<HomeScenario["onboarding"]>> = {},
): HomeScenario {
  return {
    ...homeDefaultScenario(),
    continueState: { status: "ready", data: [] },
    myWork: { status: "ready", data: [] },
    myWorkHasMore: false,
    completingIds: new Set(),
    activity: { status: "ready", data: [] },
    invitations: { status: "ready", data: [] },
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
