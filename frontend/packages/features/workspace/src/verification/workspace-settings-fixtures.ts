/**
 * Deterministic fixtures for workspace settings (WX-SET-001).
 */
import type { WorkspaceSettings } from "../core/types/workspace";
import {
  workspaceSettingsCopy,
  type WorkspaceSettingsCopy,
} from "../web/settings/workspace-settings.copy";
import type { WorkspaceSettingsSurfaceProps } from "../web/settings/workspace-settings-surface";

export function settingsCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceSettingsCopy {
  return workspaceSettingsCopy(locale);
}

export function workspaceSettingsFixture(
  overrides: Partial<WorkspaceSettings> = {},
): WorkspaceSettings {
  return {
    defaultMemberRole: "member",
    invitationExpiryDays: 7,
    allowGuestInvites: true,
    allowPublicSharing: false,
    enforceMfa: false,
    timeZone: "Asia/Ho_Chi_Minh",
    weekStart: "monday",
    ...overrides,
  };
}

type SettingsScenario = Omit<
  WorkspaceSettingsSurfaceProps,
  | "onSaveProfile"
  | "onSaveDefaults"
  | "onArchive"
  | "onDelete"
  | "onReload"
  | "onRetryDefaults"
>;

export function settingsDefaultScenario(
  locale: "en" | "vi" = "en",
): SettingsScenario {
  return {
    copy: settingsCopyFixture(locale),
    workspaceName: "Product",
    canManage: true,
    versionAvailable: true,
    profile: {
      status: "ready",
      data: {
        name: "Product",
        description: "Planning and delivery for the product team.",
      },
    },
    defaults: { status: "ready", data: workspaceSettingsFixture() },
    profileStatus: { state: "idle" },
    defaultsStatus: { state: "idle" },
    archiving: false,
    canDelete: true,
    isPersonal: false,
    deleting: false,
  };
}

/** An admin who is not the Owner: the delete action is not offered at all. */
export function settingsAdminScenario(): SettingsScenario {
  return { ...settingsDefaultScenario(), canDelete: false };
}

/** A personal workspace: the Owner sees the action explained and off. */
export function settingsPersonalScenario(): SettingsScenario {
  return { ...settingsDefaultScenario(), isPersonal: true };
}

export function settingsLoadingScenario(): SettingsScenario {
  return {
    ...settingsDefaultScenario(),
    workspaceName: null,
    profile: { status: "loading" },
    defaults: { status: "loading" },
  };
}

export function settingsErrorScenario(): SettingsScenario {
  return {
    ...settingsDefaultScenario(),
    defaults: { status: "error" },
    profileStatus: { state: "conflict" },
  };
}

export function settingsSuccessScenario(): SettingsScenario {
  return {
    ...settingsDefaultScenario(),
    profileStatus: { state: "saved" },
    defaultsStatus: { state: "saved" },
  };
}

/** A member who is not an admin: read-only, no danger zone. */
export function settingsReadOnlyScenario(): SettingsScenario {
  return { ...settingsDefaultScenario(), canManage: false };
}

/** An admin on a producer that does not report the workspace version. */
export function settingsVersionUnavailableScenario(): SettingsScenario {
  return { ...settingsDefaultScenario(), versionAvailable: false };
}

export function settingsEdgeDataScenario(): SettingsScenario {
  return {
    ...settingsDefaultScenario("vi"),
    workspaceName: "Phòng Kế hoạch & Vận hành Khu vực Đông Nam Á",
    profile: {
      status: "ready",
      data: {
        name: "Phòng Kế hoạch & Vận hành Khu vực Đông Nam Á",
        description:
          "Không gian chung của các nhóm kế hoạch, vận hành và hỗ trợ khách hàng tại Việt Nam, Thái Lan, Indonesia và Philippines. ".repeat(
            6,
          ),
      },
    },
    defaults: {
      status: "ready",
      data: workspaceSettingsFixture({
        defaultMemberRole: "guest",
        invitationExpiryDays: 30,
        allowPublicSharing: true,
        enforceMfa: true,
      }),
    },
    defaultsStatus: {
      state: "error",
      message: "Thời hạn lời mời phải nằm trong khoảng từ 1 đến 30 ngày.",
    },
  };
}
