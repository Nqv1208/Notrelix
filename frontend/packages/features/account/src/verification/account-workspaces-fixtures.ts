/**
 * Deterministic fixtures for the account "Your workspaces" page.
 */
import {
  accountWorkspacesCopy,
  type AccountWorkspacesCopy,
} from "../web/workspaces/account-workspaces.copy";
import type {
  AccountWorkspaceView,
  AccountWorkspacesSurfaceProps,
} from "../web/workspaces/account-workspaces-surface";

export function workspacesCopyFixture(
  locale: "en" | "vi" = "en",
): AccountWorkspacesCopy {
  return accountWorkspacesCopy(locale);
}

const noop = () => undefined;

export function workspaceFixture(
  overrides: Partial<AccountWorkspaceView> = {},
): AccountWorkspaceView {
  return {
    id: "workspace-primary",
    name: "Minh An Group",
    initials: "MA",
    tone: "blue",
    role: "admin",
    memberCount: 86,
    departmentCount: 5,
    isDefault: true,
    canLeave: true,
    open: { href: "/workspaces/workspace-primary", onNavigate: noop },
    ...overrides,
  };
}

export function workspaceListFixture(): AccountWorkspaceView[] {
  return [
    workspaceFixture(),
    workspaceFixture({
      id: "workspace-danang",
      name: "Minh An Đà Nẵng",
      initials: "ĐN",
      tone: "sage",
      role: "member",
      memberCount: 18,
      departmentCount: null,
      isDefault: false,
      open: { href: "/workspaces/workspace-danang", onNavigate: noop },
    }),
    workspaceFixture({
      id: "workspace-tanloc",
      name: "Tân Lộc Logistics",
      initials: "TL",
      tone: "gold",
      role: "guest",
      memberCount: null,
      departmentCount: null,
      isDefault: false,
      open: { href: "/workspaces/workspace-tanloc", onNavigate: noop },
    }),
  ];
}

export type WorkspacesScenario = Omit<
  AccountWorkspacesSurfaceProps,
  "onSetDefault" | "onLeave" | "onRetry"
>;

export function workspacesDefaultScenario(
  locale: "en" | "vi" = "en",
): WorkspacesScenario {
  return {
    copy: workspacesCopyFixture(locale),
    workspaces: { status: "ready", data: workspaceListFixture() },
    busy: false,
    actionError: null,
    notice: null,
    create: { href: "/onboarding", onNavigate: noop },
  };
}

export function workspacesLoadingScenario(
  locale: "en" | "vi" = "en",
): WorkspacesScenario {
  return {
    ...workspacesDefaultScenario(locale),
    workspaces: { status: "loading" },
  };
}

export function workspacesErrorScenario(
  locale: "en" | "vi" = "en",
): WorkspacesScenario {
  return {
    ...workspacesDefaultScenario(locale),
    workspaces: { status: "error" },
  };
}

/** The only owner of the default workspace cannot leave it. */
export function workspacesSoleOwnerScenario(
  locale: "en" | "vi" = "en",
): WorkspacesScenario {
  const [first, ...rest] = workspaceListFixture();
  return {
    ...workspacesDefaultScenario(locale),
    workspaces: {
      status: "ready",
      data: [
        {
          ...(first as AccountWorkspaceView),
          role: "owner",
          canLeave: false,
        },
        ...rest,
      ],
    },
  };
}

export function workspacesNoticeScenario(
  locale: "en" | "vi" = "en",
): WorkspacesScenario {
  return {
    ...workspacesDefaultScenario(locale),
    notice: workspacesCopyFixture(locale).notice.defaultSet.replace(
      "{{name}}",
      "Minh An Đà Nẵng",
    ),
  };
}

/** A very long name and a failed action: nothing may overflow. */
export function workspacesEdgeDataScenario(): WorkspacesScenario {
  const [first, ...rest] = workspaceListFixture();
  return {
    ...workspacesDefaultScenario("vi"),
    actionError: "failed",
    workspaces: {
      status: "ready",
      data: [
        {
          ...(first as AccountWorkspaceView),
          name: "Công ty Cổ phần Đầu tư và Phát triển Hạ tầng Công nghệ Minh An Việt Nam",
          memberCount: 1,
        },
        ...rest,
      ],
    },
  };
}
