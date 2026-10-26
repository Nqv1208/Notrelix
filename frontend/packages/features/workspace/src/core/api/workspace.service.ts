import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { parseWorkspaceRole } from "../model/members";
import { rememberMarkTone } from "../model/navigation";
import type {
  CreateWorkspaceInput,
  UpdateWorkspaceProfileInput,
  WorkspaceSettings,
  WorkspaceSummary,
} from "../types/workspace";

export interface WorkspaceApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
  put<T>(url: string, body: unknown): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
  delete<T>(url: string): Promise<T>;
}

export interface WorkspaceEndpoints {
  workspaces: {
    list: string;
    detail: (workspaceId: string) => string;
  };
  /** A workspace is created inside an Account (`Workspaces.Workspaces.CreateWorkspace`). */
  accounts: {
    workspaces: (accountId: string) => string;
  };
}

/** Endpoints of the workspace settings surface (WX-SET-001). */
export interface WorkspaceSettingsEndpoints {
  workspaces: {
    profile: (workspaceId: string) => string;
    settings: (workspaceId: string) => string;
    archive: (workspaceId: string) => string;
  };
}

type ProfileBody =
  OperationRequestBody<"Workspaces.Workspaces.UpdateWorkspaceProfile">;
type SettingsBody =
  OperationRequestBody<"Workspaces.Settings.UpdateWorkspaceSettings">;
type ArchiveBody =
  OperationRequestBody<"Workspaces.Workspaces.ArchiveWorkspace">;

/** `Workspaces.Workspaces.GetWorkspace` (and each row of the workspace lists). */
type WorkspaceDto = OperationResponse<
  "Workspaces.Workspaces.GetWorkspace",
  200
>;
type WorkspaceSettingsDto = OperationResponse<
  "Workspaces.Settings.GetWorkspaceSettings",
  200
>;

/**
 * Reads the workspace record. `version` is the aggregate version the profile,
 * settings and archive writes send back; it is kept only when it is a
 * positive number, so a malformed row can never produce a write.
 */
export function parseWorkspaceSummary(payload: unknown): WorkspaceSummary {
  const summary = payload as Omit<WorkspaceSummary, "version"> &
    Pick<WorkspaceDto, "version">;
  const { version, ...rest } = summary;
  rememberMarkTone(rest.id, (payload as { color?: unknown }).color);
  return typeof version === "number" && version > 0
    ? { ...rest, version }
    : rest;
}

/**
 * `GetWorkspaceSettings` returns `WorkspaceSettingsDto`; unknown values fall
 * back to the producer's defaults only for display, never for a write.
 */
export function parseWorkspaceSettings(payload: unknown): WorkspaceSettings {
  const raw = (payload ?? {}) as Partial<
    Record<keyof WorkspaceSettingsDto, unknown>
  >;
  const role = parseWorkspaceRole(raw.defaultMemberRole);
  return {
    defaultMemberRole: role && role !== "owner" ? role : "member",
    invitationExpiryDays:
      typeof raw.invitationExpiryDays === "number"
        ? raw.invitationExpiryDays
        : 7,
    allowGuestInvites: raw.allowGuestInvites === true,
    allowPublicSharing: raw.allowPublicSharing === true,
    enforceMfa: raw.enforceMfa === true,
    timeZone:
      typeof raw.timeZone === "string" && raw.timeZone.trim()
        ? raw.timeZone
        : "Asia/Ho_Chi_Minh",
    weekStart: raw.weekStart === "sunday" ? "sunday" : "monday",
  };
}

const ROLE_NAME = { admin: "Admin", member: "Member", guest: "Guest" } as const;

export function createWorkspaceService(
  api: WorkspaceApiClient,
  endpoints: WorkspaceEndpoints,
) {
  return {
    async getList(): Promise<WorkspaceSummary[]> {
      const rows = await api.get<unknown>(endpoints.workspaces.list);
      return Array.isArray(rows) ? rows.map(parseWorkspaceSummary) : [];
    },

    async getDetail(id: string): Promise<WorkspaceSummary> {
      return parseWorkspaceSummary(
        await api.get<unknown>(endpoints.workspaces.detail(id)),
      );
    },

    /**
     * Creates the workspace in the Account (the producer answers with its id),
     * then reads it back so the caller has the summary the lists draw.
     */
    async create(
      accountId: string,
      input: CreateWorkspaceInput,
    ): Promise<WorkspaceSummary> {
      const id = await api.post<string>(
        endpoints.accounts.workspaces(accountId),
        input,
      );
      return parseWorkspaceSummary(
        await api.get<unknown>(endpoints.workspaces.detail(id)),
      );
    },
  };
}

/**
 * Profile, defaults and archive (`Workspaces.Workspaces.UpdateWorkspaceProfile`,
 * `Workspaces.Settings.*`, `Workspaces.Workspaces.ArchiveWorkspace`). Every
 * write carries the workspace version the member last saw.
 */
export function createWorkspaceSettingsService(
  api: WorkspaceApiClient,
  endpoints: WorkspaceSettingsEndpoints,
) {
  return {
    async updateProfile(
      workspaceId: string,
      input: UpdateWorkspaceProfileInput,
    ): Promise<void> {
      const body: ProfileBody = {
        name: input.name.trim(),
        description: input.description.trim(),
        expectedVersion: input.expectedVersion,
      };
      await api.patch<void>(endpoints.workspaces.profile(workspaceId), body);
    },

    async getSettings(workspaceId: string): Promise<WorkspaceSettings> {
      return parseWorkspaceSettings(
        await api.get<unknown>(endpoints.workspaces.settings(workspaceId)),
      );
    },

    async updateSettings(
      workspaceId: string,
      settings: WorkspaceSettings,
      expectedVersion: number,
    ): Promise<void> {
      const body: SettingsBody = {
        allowPublicSharing: settings.allowPublicSharing,
        enforceMfa: settings.enforceMfa,
        allowGuestInvites: settings.allowGuestInvites,
        defaultMemberRole: ROLE_NAME[settings.defaultMemberRole],
        invitationExpiryDays: settings.invitationExpiryDays,
        timeZone: settings.timeZone,
        weekStart: settings.weekStart,
        expectedVersion,
      };
      await api.put<void>(endpoints.workspaces.settings(workspaceId), body);
    },

    async archive(workspaceId: string, expectedVersion: number): Promise<void> {
      const body: ArchiveBody = { expectedVersion };
      await api.post<void>(endpoints.workspaces.archive(workspaceId), body);
    },
  };
}

export type WorkspaceSettingsService = ReturnType<
  typeof createWorkspaceSettingsService
>;
