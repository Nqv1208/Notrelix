import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import type {
  BoardTemplateInfo,
  CompanySize,
  DepartmentColor,
  InviteRole,
  OnboardingStepId,
} from "@notrelix/features-workspace/web";

/** `api` is the runtime client; every call is quiet: the pages say what went wrong themselves. */
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
  patch<T>(
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

export interface DraftDto {
  readonly draft: {
    readonly step: Exclude<OnboardingStepId, "done">;
    readonly workspaceId: string | null;
    readonly data: Record<string, unknown>;
  } | null;
}

export interface JoinDto {
  readonly workspace: { id: string; name: string; memberCount: number };
  /** Null: the producer does not say who invited the person (the page words it without a name). */
  readonly inviterName: string | null;
  readonly role: string;
  readonly spaces: readonly {
    id: string;
    name: string;
    color: DepartmentColor | null;
    open: boolean;
    access: string | null;
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
    key: "first-item" | "invite-more";
    done: boolean;
    boardId: string | null;
    boardTitle: string | null;
  }[];
}

type DraftOutput = OperationResponse<
  "Workspaces.Onboarding.GetOnboardingDraft",
  200
>;
type BootstrapOutput = OperationResponse<"Identity.Auth.GetBootstrap", 200>;
type SlugCheckOutput = OperationResponse<
  "Workspaces.Workspaces.CheckWorkspaceSlug",
  200
>;
type CreateWorkspaceBody =
  OperationRequestBody<"Workspaces.Workspaces.CreateWorkspace">;
type WorkspaceOutput = OperationResponse<
  "Workspaces.Workspaces.GetWorkspace",
  200
>;
type MembershipOutput = OperationResponse<
  "Workspaces.Members.GetMyMembership",
  200
>;
type SpaceTreeOutput = OperationResponse<"Workspaces.Spaces.GetSpaceTree", 200>;
type BoardsOutput = OperationResponse<
  "WorkManagement.Boards.ListWorkspaceBoards",
  200
>;
type CurrentUserOutput = OperationResponse<"Identity.Auth.GetCurrentUser", 200>;
type PreferencesOutput = OperationResponse<
  "Collaboration.Preferences.GetNotificationPreferences",
  200
>;
type GettingStartedOutput = OperationResponse<
  "Workspaces.Onboarding.GetGettingStarted",
  200
>;
type SpaceBody = OperationRequestBody<"Workspaces.Spaces.CreateSpace">;
type InviteBody = OperationRequestBody<"Workspaces.Members.InviteMember">;

/** The producer's company-size bands for the sizes the pages offer. */
const SIZE_BAND: Record<CompanySize, string> = {
  s: "1-10",
  m: "11-50",
  l: "51-200",
  xl: "201-1000",
  xxl: "1000+",
};

/** What a person's role in a Space lets them do on its Boards. */
function boardLevelOf(access: string | null | undefined) {
  switch (access?.toLowerCase()) {
    case "manager":
    case "editor":
      return "editor" as const;
    case "commenter":
      return "commenter" as const;
    default:
      return "viewer" as const;
  }
}

/** The join page's two email choices, read from the member's own notification matrix. */
function notifyChoiceOf(preferences: PreferencesOutput | undefined) {
  const digest = preferences?.rows?.find((row) => row.type === "digest");
  return digest?.email === false ? "mentions" : "digest";
}

/** A refusal the producer answers for one row of a batch (not a failed call). */
const isRowRefusal = (failure: unknown): boolean =>
  [400, 409, 422].includes((failure as { status?: number }).status ?? 0);

/**
 * The calls behind the onboarding pages. The draft, the departments (one
 * `CreateSpace` each), the invitations (one `InviteMember` each, with the
 * department and its role as an initial entry), the first Board and the
 * getting-started card use the producer's operations, and so do the address
 * check and the workspace create, which run in the Account the bootstrap names.
 * The first visit of an invited person is composed from the producer's reads
 * (workspace, membership, Space tree, Boards, notification preferences) and
 * its writes (profile name, notification preferences); it has no contract of
 * its own. The department suggestions are the page's starting points, not a
 * server read (see `onboardingSuggestions`).
 */
export function createOnboardingApi(api: OnboardingApiClient) {
  const key = () => crypto.randomUUID();
  // The workspace create keeps its key while the same request is retried, so a retry after a lost
  // answer returns the workspace the first attempt made instead of a second one.
  let creating: { signature: string; key: string } | null = null;
  // The member's Account, read once from the bootstrap (a failure is not kept).
  let account: Promise<string> | null = null;
  const accountId = (): Promise<string> => {
    account ??= api.get<BootstrapOutput>(endpoints.auth.bootstrap, QUIET).then(
      (bootstrap) => {
        if (!bootstrap?.accountId) throw new Error("The member has no Account");
        return bootstrap.accountId;
      },
      (failure: unknown) => {
        account = null;
        throw failure;
      },
    );
    return account;
  };
  return {
    async slugCheck(slug: string): Promise<SlugCheckDto> {
      const checked = await api.get<SlugCheckOutput>(
        `${endpoints.accounts.workspaceSlugCheck(await accountId())}?slug=${encodeURIComponent(slug)}`,
        QUIET,
      );
      const free = checked?.status === "free";
      return {
        available: free,
        reason: free ? null : checked?.status === "taken" ? "taken" : "invalid",
        suggestion: checked?.suggestion ?? null,
      };
    },
    async getDraft(): Promise<DraftDto> {
      const found = await api.get<DraftOutput>(
        endpoints.onboarding.draft,
        QUIET,
      );
      const draft = found?.draft;
      if (!draft?.step) return { draft: null };
      return {
        draft: {
          step: draft.step as NonNullable<DraftDto["draft"]>["step"],
          workspaceId: draft.workspaceId ?? null,
          data: (draft.data ?? {}) as Record<string, unknown>,
        },
      };
    },
    saveDraft(draft: NonNullable<DraftDto["draft"]>) {
      const body: OperationRequestBody<"Workspaces.Onboarding.SaveOnboardingDraft"> =
        {
          step: draft.step,
          workspaceId: draft.workspaceId,
          data: draft.data,
        };
      return api.put<void>(endpoints.onboarding.draft, body, QUIET);
    },
    clearDraft() {
      return api.delete<void>(endpoints.onboarding.draft, QUIET);
    },
    async createWorkspace(input: {
      name: string;
      slug: string;
      size: CompanySize;
      locale: "vi" | "en";
    }) {
      const body: CreateWorkspaceBody = {
        name: input.name,
        slug: input.slug,
        size: SIZE_BAND[input.size],
        language: input.locale,
      };
      const signature = JSON.stringify(body);
      if (creating?.signature !== signature)
        creating = { signature, key: key() };
      const id = await api.post<string>(
        endpoints.accounts.workspaces(await accountId()),
        body,
        { ...QUIET, idempotencyKey: creating.key },
      );
      creating = null;
      return { id, slug: input.slug };
    },
    /**
     * One private Department per entry, in order. The producer has no
     * department colour yet, so the colour chosen is not stored.
     */
    async createDepartments(
      workspaceId: string,
      departments: readonly { name: string; color: DepartmentColor }[],
    ) {
      const made: { id: string; name: string; color: DepartmentColor }[] = [];
      for (const department of departments) {
        const body: SpaceBody = {
          name: department.name,
          visibility: "Private",
          spaceType: "Department",
        };
        const id = await api.post<string>(
          endpoints.workspaces.spaces(workspaceId),
          body,
          QUIET,
        );
        made.push({ id, name: department.name, color: department.color });
      }
      return { departments: made };
    },
    /** One workspace invitation as Member each, with the department and role as the initial entry. */
    async sendInvites(
      workspaceId: string,
      invites: readonly {
        email: string;
        spaceId: string | null;
        role: InviteRole;
      }[],
    ) {
      let sent = 0;
      const skipped: string[] = [];
      for (const invite of invites) {
        const body: InviteBody = {
          email: invite.email,
          role: "Member",
          initialEntries: invite.spaceId
            ? [{ kind: "Space", targetId: invite.spaceId, role: invite.role }]
            : [],
        };
        try {
          await api.post<string>(
            endpoints.workspaces.members(workspaceId),
            body,
            QUIET,
          );
          sent += 1;
        } catch (failure) {
          if (!isRowRefusal(failure)) throw failure;
          skipped.push(invite.email.toLowerCase());
        }
      }
      return { sent, skipped };
    },
    /** A blank Board: the producer has no onboarding template catalogue, so the template is not applied. */
    async createBoard(
      workspaceId: string,
      input: { templateId: string; name: string; spaceId: string | null },
    ) {
      const body: OperationRequestBody<"WorkManagement.Boards.Create"> = {
        title: input.name,
        ...(input.spaceId ? { spaceId: input.spaceId } : {}),
      };
      const id = await api.post<unknown>(
        endpoints.boards.listByWorkspaceId(workspaceId),
        body,
        { ...QUIET, idempotencyKey: key() },
      );
      return {
        id: typeof id === "string" ? id : "",
        templateId: input.templateId,
      };
    },
    async getJoin(workspaceId: string): Promise<JoinDto> {
      const [workspace, membership, tree, boards, user, preferences] =
        await Promise.all([
          api.get<WorkspaceOutput>(
            endpoints.workspaces.detail(workspaceId),
            QUIET,
          ),
          api.get<MembershipOutput>(
            endpoints.workspaces.myMembership(workspaceId),
            QUIET,
          ),
          api.get<SpaceTreeOutput>(
            endpoints.workspaces.spaceTree(workspaceId),
            QUIET,
          ),
          api.get<BoardsOutput>(
            endpoints.boards.listByWorkspaceId(workspaceId),
            QUIET,
          ),
          api.get<CurrentUserOutput>(endpoints.auth.profile, QUIET),
          // The matrix is optional: without it the page offers the default.
          api
            .get<PreferencesOutput>(
              endpoints.notifications.preferences(workspaceId),
              QUIET,
            )
            .catch(() => undefined),
        ]);
      const readable = (boards ?? []).filter((board) => !board.isArchived);
      const spaces = (tree ?? [])
        .filter((space) => !space.parentSpaceId && !space.isArchived)
        .map((space) => {
          const level = boardLevelOf(space.accessLevel);
          return {
            id: space.id ?? "",
            name: space.name ?? "",
            color: null,
            open: space.visibility !== "Private",
            access: space.accessLevel ?? null,
            boards: readable
              .filter((board) => board.spaceId === space.id)
              .map((board) => ({
                id: board.id ?? "",
                title: board.title ?? "",
                level,
              })),
          };
        });
      return {
        workspace: {
          id: workspace?.id ?? workspaceId,
          name: workspace?.name ?? "",
          memberCount: workspace?.memberCount ?? 0,
        },
        inviterName: null,
        role: (membership?.role ?? "member").toLowerCase(),
        spaces,
        profile: {
          displayName: user?.name ?? "",
          notifyEmail: notifyChoiceOf(preferences),
        },
        companyAccent: "blue",
      };
    },
    /**
     * The name goes to the person's profile and the email choice to their
     * notification matrix of this workspace: "digest" keeps the daily digest
     * email, "mentions" turns it off; a mention always emails.
     */
    async saveJoin(
      workspaceId: string,
      input: { displayName: string; notifyEmail: "digest" | "mentions" },
    ) {
      const current = await api.get<PreferencesOutput>(
        endpoints.notifications.preferences(workspaceId),
        QUIET,
      );
      await api.patch<unknown>(
        endpoints.users.updateProfile,
        { name: input.displayName },
        QUIET,
      );
      await api.patch<unknown>(
        endpoints.notifications.preferences(workspaceId),
        {
          expectedVersion: current?.version ?? 0,
          rows: [
            { type: "digest", email: input.notifyEmail === "digest" },
            { type: "mention", email: true },
          ],
        },
        QUIET,
      );
    },
    async gettingStarted(workspaceId: string): Promise<GettingStartedDto> {
      const card = await api.get<GettingStartedOutput>(
        endpoints.onboarding.gettingStarted(workspaceId),
        QUIET,
      );
      return {
        visible: card?.visible ?? false,
        items: (card?.items ?? []).flatMap((item) =>
          item.key === "first-item" || item.key === "invite-more"
            ? [
                {
                  key: item.key,
                  done: item.done ?? false,
                  boardId: item.boardId ?? null,
                  boardTitle: item.boardTitle ?? null,
                },
              ]
            : [],
        ),
      };
    },
    dismissGettingStarted(workspaceId: string) {
      return api.post<void>(
        endpoints.onboarding.dismissGettingStarted(workspaceId),
        {},
        { ...QUIET, idempotencyKey: key() },
      );
    },
  };
}

export type OnboardingApi = ReturnType<typeof createOnboardingApi>;
export type { BoardTemplateInfo };
