import type {
  BoardTemplateInfo,
  CompanySize,
  DepartmentColor,
  InviteRole,
  OnboardingCatalog,
  OnboardingStepId,
} from "@notrelix/features-workspace/web";

/**
 * The calls behind the onboarding pages (gap `CTR-GAP-ONBOARD-FLOW`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Onboarding"). `api` is the runtime client; every call is quiet: the pages
 * say what went wrong themselves.
 */
export interface OnboardingApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean; idempotencyKey?: string },
  ): Promise<T>;
  put<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
  delete<T>(
    url: string,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
}

const QUIET = { skipGlobalErrorToast: true } as const;

export interface SlugCheckDto {
  readonly available: boolean;
  readonly reason: "taken" | "invalid" | null;
  readonly suggestion: string | null;
}

export interface CatalogDto extends OnboardingCatalog {
  readonly size: CompanySize;
  readonly departments: readonly { key: string; name: string }[];
  readonly colors: readonly DepartmentColor[];
}

export interface DraftDto {
  readonly draft: {
    readonly step: Exclude<OnboardingStepId, "done">;
    readonly workspaceId: string | null;
    readonly data: Record<string, unknown>;
  } | null;
}

export interface JoinDto {
  readonly workspace: { id: string; name: string; memberCount: number };
  readonly inviterName: string;
  readonly role: string;
  readonly spaces: readonly {
    id: string;
    name: string;
    color: DepartmentColor | null;
    open: boolean;
    access: "Manager" | "Editor" | "Commenter" | "Viewer" | string | null;
    boards: readonly {
      id: string;
      title: string;
      level: "viewer" | "commenter" | "editor";
    }[];
  }[];
  readonly profile: {
    displayName: string;
    notifyEmail: "digest" | "mentions";
  };
  readonly companyAccent: string;
}

export interface GettingStartedDto {
  readonly visible: boolean;
  readonly items: readonly {
    key: "first-item" | "invite-more" | "mobile";
    done: boolean;
    boardId: string | null;
    boardTitle: string | null;
    pendingInvites: number | null;
  }[];
}

export function createOnboardingApi(api: OnboardingApiClient) {
  const key = () => crypto.randomUUID();
  return {
    slugCheck(slug: string) {
      return api.get<SlugCheckDto>(
        `/onboarding/slug-check?slug=${encodeURIComponent(slug)}`,
        QUIET,
      );
    },
    catalog(size: CompanySize, name: string) {
      return api.get<CatalogDto>(
        `/onboarding/catalog?size=${size}&name=${encodeURIComponent(name)}`,
        QUIET,
      );
    },
    getDraft() {
      return api.get<DraftDto>("/onboarding/draft", QUIET);
    },
    saveDraft(draft: NonNullable<DraftDto["draft"]>) {
      return api.put<void>("/onboarding/draft", draft, QUIET);
    },
    clearDraft() {
      return api.delete<void>("/onboarding/draft", QUIET);
    },
    createWorkspace(input: {
      name: string;
      slug: string;
      size: CompanySize;
      locale: "vi" | "en";
    }) {
      return api.post<{ id: string; slug: string }>(
        "/onboarding/workspace",
        input,
        { ...QUIET, idempotencyKey: key() },
      );
    },
    createDepartments(
      workspaceId: string,
      departments: readonly { name: string; color: DepartmentColor }[],
    ) {
      return api.post<{
        departments: { id: string; name: string; color: DepartmentColor }[];
      }>(
        `/workspaces/${workspaceId}/onboarding/departments`,
        { departments },
        { ...QUIET, idempotencyKey: key() },
      );
    },
    sendInvites(
      workspaceId: string,
      invites: readonly {
        email: string;
        spaceId: string | null;
        role: InviteRole;
      }[],
    ) {
      return api.post<{ sent: number; skipped: string[] }>(
        `/workspaces/${workspaceId}/onboarding/invitations`,
        { invites },
        { ...QUIET, idempotencyKey: key() },
      );
    },
    createBoard(
      workspaceId: string,
      input: { templateId: string; name: string; spaceId: string | null },
    ) {
      return api.post<{ id: string; templateId: string }>(
        `/workspaces/${workspaceId}/onboarding/board`,
        input,
        { ...QUIET, idempotencyKey: key() },
      );
    },
    getJoin(workspaceId: string) {
      return api.get<JoinDto>(
        `/workspaces/${workspaceId}/onboarding/join`,
        QUIET,
      );
    },
    saveJoin(
      workspaceId: string,
      input: { displayName: string; notifyEmail: "digest" | "mentions" },
    ) {
      return api.put<void>(
        `/workspaces/${workspaceId}/onboarding/join`,
        input,
        QUIET,
      );
    },
    gettingStarted(workspaceId: string) {
      return api.get<GettingStartedDto>(
        `/workspaces/${workspaceId}/getting-started`,
        QUIET,
      );
    },
    dismissGettingStarted(workspaceId: string) {
      return api.post<void>(
        `/workspaces/${workspaceId}/getting-started/dismiss`,
        {},
        { ...QUIET, idempotencyKey: key() },
      );
    },
    completeGettingStarted(workspaceId: string, itemKey: string) {
      return api.post<void>(
        `/workspaces/${workspaceId}/getting-started/${itemKey}/done`,
        {},
        { ...QUIET, idempotencyKey: key() },
      );
    },
  };
}

export type OnboardingApi = ReturnType<typeof createOnboardingApi>;
export type { BoardTemplateInfo };
