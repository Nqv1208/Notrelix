/**
 * Deterministic fixtures and scenarios for the Notrelix system rail
 * (Final-Space `nav=hien-de` / `nav=hien-day`). No clocks, no randomness,
 * no network. Mockup world data (Minh An Group, Nguyễn Thu Hà) lives only
 * here and in stories.
 */
import type { ShellUser } from "../web/shell/shell-models";
import {
  notrelixSystemRailCopy,
  type NotrelixSystemRailCopy,
} from "../web/shell/notrelix-system-rail.copy";
import type {
  NotrelixSystemRailSurfaceProps,
  SystemRailWorkspace,
} from "../web/shell/notrelix-system-rail-surface";

export function systemRailCopyFixture(
  locale: "en" | "vi" = "en",
): NotrelixSystemRailCopy {
  return notrelixSystemRailCopy(locale);
}

export function systemRailWorkspaceFixture(
  overrides: Partial<SystemRailWorkspace> = {},
): SystemRailWorkspace {
  return {
    id: "ws-minh-an",
    name: "Minh An Group",
    shortName: "Minh An",
    initials: "MA",
    tone: "blue",
    isGuest: false,
    unread: null,
    current: false,
    ...overrides,
  };
}

/** The three workspaces of Final-Space: current, one with unread, one guest. */
export function systemRailWorkspacesFixture(): readonly SystemRailWorkspace[] {
  return [
    systemRailWorkspaceFixture({ current: true }),
    systemRailWorkspaceFixture({
      id: "ws-minh-an-da-nang",
      name: "Minh An Đà Nẵng",
      shortName: "Đà Nẵng",
      initials: "ĐN",
      tone: "sage",
      unread: 2,
    }),
    systemRailWorkspaceFixture({
      id: "ws-tan-loc",
      name: "Tân Lộc Logistics",
      shortName: "Tân Lộc",
      initials: "TL",
      tone: "gold",
      isGuest: true,
    }),
  ];
}

export function systemRailUserFixture(
  overrides: Partial<ShellUser> = {},
): ShellUser {
  return {
    id: "user-thu-ha",
    name: "Nguyễn Thu Hà",
    email: "thuha@minhan.vn",
    avatarUrl: null,
    identity: "sage",
    ...overrides,
  };
}

export type SystemRailScenario = Omit<
  NotrelixSystemRailSurfaceProps,
  | "onSwitchWorkspace"
  | "onCreateWorkspace"
  | "onOpenAllInbox"
  | "onOpenGlobalSearch"
  | "onOpenAccount"
  | "onTogglePinned"
  | "onClose"
>;

/** Overlay (`nav=hien-de`). The all-workspace inbox has no backend yet. */
export function systemRailOverlayScenario(
  locale: "en" | "vi" = "en",
): SystemRailScenario {
  return {
    copy: systemRailCopyFixture(locale),
    presentation: "overlay",
    workspaces: systemRailWorkspacesFixture(),
    user: systemRailUserFixture(),
    allInboxAvailable: false,
    allInboxUnread: null,
  };
}

/** Pinned (`nav=hien-day`): in flow, pushes the layout. */
export function systemRailPinnedScenario(
  locale: "en" | "vi" = "en",
): SystemRailScenario {
  return { ...systemRailOverlayScenario(locale), presentation: "pinned" };
}

/**
 * Drawer (narrow screens): the workspaces and Add as a row at the top of the
 * off-canvas navigation sheet, which owns focus and dismissal.
 */
export function systemRailDrawerScenario(
  locale: "en" | "vi" = "en",
): SystemRailScenario {
  return { ...systemRailOverlayScenario(locale), presentation: "drawer" };
}

export function systemRailHiddenScenario(
  locale: "en" | "vi" = "en",
): SystemRailScenario {
  return { ...systemRailOverlayScenario(locale), presentation: "hidden" };
}

/**
 * Eight workspaces: long names, unread 0 / 3 / 120 / null, a guest
 * membership, the all-workspace inbox available with 120 unread, no user.
 */
export function systemRailEdgeDataScenario(
  locale: "en" | "vi" = "en",
): SystemRailScenario {
  return {
    ...systemRailOverlayScenario(locale),
    presentation: "pinned",
    workspaces: [
      systemRailWorkspaceFixture({ current: true, unread: 0 }),
      systemRailWorkspaceFixture({
        id: "ws-long-1",
        name: "Công ty Cổ phần Phát triển Hạ tầng Kỹ thuật Đô thị Miền Trung",
        shortName: "Hạ tầng Kỹ thuật Miền Trung",
        initials: "HT",
        tone: "coral",
        unread: 3,
      }),
      systemRailWorkspaceFixture({
        id: "ws-long-2",
        name: "Tân Lộc Logistics và Chuỗi cung ứng Quốc tế",
        shortName: "Tân Lộc Logistics",
        initials: "TL",
        tone: "gold",
        isGuest: true,
        unread: 120,
      }),
      systemRailWorkspaceFixture({
        id: "ws-4",
        name: "Minh An Đà Nẵng",
        shortName: "Đà Nẵng",
        initials: "ĐN",
        tone: "sage",
        unread: null,
      }),
      systemRailWorkspaceFixture({
        id: "ws-5",
        name: "Phòng thí nghiệm Sản phẩm",
        shortName: "Sản phẩm",
        initials: "SP",
        tone: "crimson",
        unread: 0,
      }),
      systemRailWorkspaceFixture({
        id: "ws-6",
        name: "Ban Quản lý Dự án Vành đai 3",
        shortName: "Vành đai 3",
        initials: "VĐ",
        tone: "violet",
        unread: 7,
      }),
      systemRailWorkspaceFixture({
        id: "ws-7",
        name: "Quỹ Học bổng Minh An",
        shortName: "Học bổng",
        initials: "HB",
        tone: "blue",
        isGuest: true,
        unread: null,
      }),
      systemRailWorkspaceFixture({
        id: "ws-8",
        name: "Văn phòng Hồ Chí Minh",
        shortName: "Hồ Chí Minh",
        initials: "HC",
        tone: "coral",
        unread: 99,
      }),
    ],
    user: null,
    allInboxAvailable: true,
    allInboxUnread: 120,
  };
}
