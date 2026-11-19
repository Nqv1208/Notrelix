import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { parseWorkspaceRole } from "../model/members";
import { rememberMarkTone } from "../model/navigation";
import type {
  CreateWorkspaceInput,
  UpdateWorkspaceProfileInput,
  WorkspaceFigures,
  WorkspaceSettings,
  WorkspaceSummary,
} from "../types/workspace";

export interface WorkspaceApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body: unknown,
    options?: { idempotencyKey?: string; skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  put<T>(url: string, body: unknown): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
  delete<T>(url: string): Promise<T>;
}

export interface WorkspaceEndpoints {
  workspaces: {
    list: string;
    detail: (workspaceId: string) => string;
    /** `Workspaces.Workspaces.GetWorkspaceDashboard` (GET, Owner and Admin). */
    dashboard: (workspaceId: string) => string;
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
    slug: (workspaceId: string) => string;
    settings: (workspaceId: string) => string;
    archive: (workspaceId: string) => string;
    unarchive: (workspaceId: string) => string;
    /** The delete write quotes the workspace version in the query (`DeleteWorkspace`). */
    remove: (workspaceId: string, expectedVersion: number) => string;
    transferOwnership: (workspaceId: string) => string;
  };
}

type ProfileBody =
  OperationRequestBody<"Workspaces.Workspaces.UpdateWorkspaceProfile">;
type SlugBody =
  OperationRequestBody<"Workspaces.Workspaces.ChangeWorkspaceSlug">;
type SettingsBody =
  OperationRequestBody<"Workspaces.Settings.UpdateWorkspaceSettings">;
type ArchiveBody =
  OperationRequestBody<"Workspaces.Workspaces.ArchiveWorkspace">;
type UnarchiveBody =
  OperationRequestBody<"Workspaces.Workspaces.UnarchiveWorkspace">;
type TransferOwnershipBody =
  OperationRequestBody<"Workspaces.Workspaces.TransferOwnership">;

/** `Workspaces.Workspaces.GetWorkspace` (and each row of the workspace lists). */
type WorkspaceDto = OperationResponse<
  "Workspaces.Workspaces.GetWorkspace",
  200
>;
type WorkspaceDashboardDto = OperationResponse<
  "Workspaces.Workspaces.GetWorkspaceDashboard",
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
    guestDefaultExpiryDays:
      raw.guestDefaultExpiryDays === 30 ||
      raw.guestDefaultExpiryDays === 90 ||
      raw.guestDefaultExpiryDays === 180
        ? raw.guestDefaultExpiryDays
        : null,
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

    /**
     * The workspace figures for Owner and Admin (`GetWorkspaceDashboard`): active members, the Spaces the caller
     * can see, Teams and pending invitations. A Member or Guest is refused with 403; a count the producer does
     * not send reads as 0.
     */
    async getFigures(id: string): Promise<WorkspaceFigures> {
      const raw = (await api.get<unknown>(endpoints.workspaces.dashboard(id), {
        skipGlobalErrorToast: true,
      })) as Partial<Record<keyof WorkspaceDashboardDto, unknown>> | null;
      const count = (value: unknown) =>
        typeof value === "number" && value >= 0 ? value : 0;
      return {
        members: count(raw?.memberCount),
        spaces: count(raw?.spaceCount),
        teams: count(raw?.teamCount),
        invitations: count(raw?.invitationCount),
        archived: raw?.isArchived === true,
      };
    },

    async getDetail(id: string): Promise<WorkspaceSummary> {
      return parseWorkspaceSummary(
        await api.get<unknown>(endpoints.workspaces.detail(id)),
      );
    },

    /**
     * Creates the workspace in the Account (the producer answers with its id),
     * then reads it back so the caller has the summary the lists draw. The
     * same `idempotencyKey` replays the first attempt, so a retry never makes a
     * second workspace.
     */
    async create(
      accountId: string,
      input: CreateWorkspaceInput,
      idempotencyKey?: string,
    ): Promise<WorkspaceSummary> {
      const id = await api.post<string>(
        endpoints.accounts.workspaces(accountId),
        input,
        idempotencyKey ? { idempotencyKey } : undefined,
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

    /**
     * Changes the workspace address (`ChangeWorkspaceSlug`, Owner only). Answers 204; a version that moved
     * meanwhile answers 412, a taken address 409 `workspaces.workspace.slug-taken`, a rule break 400. Links use
     * ids, so the old address is released and nothing redirects.
     */
    async changeSlug(
      workspaceId: string,
      slug: string,
      expectedVersion: number,
    ): Promise<void> {
      const body: SlugBody = {
        slug: slug.trim().toLowerCase(),
        expectedVersion,
      };
      await api.patch<void>(endpoints.workspaces.slug(workspaceId), body);
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
        guestDefaultExpiryDays: settings.guestDefaultExpiryDays,
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

    /**
     * Brings an archived workspace back (Owner only: lifecycle is not lent to an Admin). Quotes the
     * workspace version the member last saw; answers 204, 412 when it moved meanwhile, 403 for anyone
     * but the Owner.
     */
    async unarchive(
      workspaceId: string,
      expectedVersion: number,
    ): Promise<void> {
      const body: UnarchiveBody = { expectedVersion };
      await api.post<void>(endpoints.workspaces.unarchive(workspaceId), body);
    },

    /**
     * Deletes the workspace (Owner only; `DeleteWorkspace`). It is restorable for 30 days from the
     * Recently deleted list, then the producer purges it. Answers 204; a version that moved meanwhile
     * answers 412 and a personal workspace is refused with its rule code.
     */
    async remove(workspaceId: string, expectedVersion: number): Promise<void> {
      await api.delete<void>(
        endpoints.workspaces.remove(workspaceId, expectedVersion),
      );
    },

    /**
     * Hands the workspace to another member (`TransferOwnership`): the person becomes Owner and the caller
     * Admin. Needs a fresh single-use step-up proof for `TransferWorkspaceOwnership`: none answers 412
     * `identity.security.step-up-required`, a stale, spent or foreign one 400 `identity.security.step-up-invalid`.
     * Answers 204; a version that moved meanwhile answers 412, a Guest or a person who is not an
     * active member is refused, and only an Owner may ask.
     */
    async transferOwnership(
      workspaceId: string,
      newOwnerId: string,
      expectedVersion: number,
      stepUpToken: string,
    ): Promise<void> {
      const body: TransferOwnershipBody = {
        newOwnerId,
        expectedVersion,
        stepUpToken,
      };
      await api.post<void>(
        endpoints.workspaces.transferOwnership(workspaceId),
        body,
        // Every refusal is said in the dialog, a stale or missing proof included.
        { skipGlobalErrorToast: true },
      );
    },
  };
}

export type WorkspaceSettingsService = ReturnType<
  typeof createWorkspaceSettingsService
>;
