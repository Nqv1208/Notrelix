import type { OperationRequestBody } from "@notrelix/contracts";
import { parseWorkspaceRole } from "../model/members";
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

/** Reads the workspace record; `version` is kept only when it is a number. */
export function parseWorkspaceSummary(payload: unknown): WorkspaceSummary {
  const summary = payload as WorkspaceSummary & { version?: unknown };
  const { version, ...rest } = summary;
  return typeof version === "number" && version > 0
    ? { ...rest, version }
    : rest;
}

/**
 * `GetWorkspaceSettings` returns `WorkspaceSettingsDto` (typed `System.Void`
 * in the generated contract); unknown values fall back to the producer's
 * defaults only for display, never for a write.
 */
export function parseWorkspaceSettings(payload: unknown): WorkspaceSettings {
  const raw = (payload ?? {}) as Record<string, unknown>;
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
  };
}

const ROLE_NAME = { admin: "Admin", member: "Member", guest: "Guest" } as const;

export function createWorkspaceService(
  api: WorkspaceApiClient,
  endpoints: WorkspaceEndpoints,
) {
  return {
    async getList(): Promise<WorkspaceSummary[]> {
      return api.get<WorkspaceSummary[]>(endpoints.workspaces.list);
    },

    async getDetail(id: string): Promise<WorkspaceSummary> {
      return parseWorkspaceSummary(
        await api.get<unknown>(endpoints.workspaces.detail(id)),
      );
    },

    async create(input: CreateWorkspaceInput): Promise<WorkspaceSummary> {
      return api.post<WorkspaceSummary>(endpoints.workspaces.list, input);
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
