/**
 * Workspaces — the onboarding pages. The canonical operations:
 *
 *   Workspaces.Onboarding.Get/Save/ClearOnboardingDraft — GET/PUT/DELETE /workspaces/onboarding-draft
 *   Workspaces.Onboarding.GetGettingStarted / DismissGettingStarted
 *
 * (departments, invitations and the first board go through CreateSpace,
 * InviteMember and Boards.Create; the address check and the workspace create
 * are the Account's routes, Workspaces.Workspaces.*.) The department and
 * board-template suggestions are the page's own starting points, and the
 * first visit of an invited person is composed from the workspace, membership,
 * Space tree, Boards, profile and notification-preference operations: none of
 * them is an operation of its own.
 */
import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  created,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  SLUG_PATTERN,
  isSlugTaken,
  suggestSlug,
  type OnboardingDraft,
} from "../../state/mock-onboarding";
import type { MockStore } from "../../state/mock-store";

/** Addresses no workspace may take (Workspaces config). */
const RESERVED_SLUGS = new Set([
  "admin",
  "api",
  "app",
  "www",
  "settings",
  "login",
  "signup",
  "billing",
  "support",
  "help",
  "new",
  "invite",
  "join",
  "onboarding",
  "workspaces",
]);
const SIZE_BANDS = ["1-10", "11-50", "51-200", "201-1000", "1000+"];

/** The rule code the address breaks, or null (3 to 63 characters, not reserved). */
function slugViolation(slug: string): string | null {
  if (!SLUG_PATTERN.test(slug)) return "SharedKernel_Slug_InvalidFormat";
  if (slug.length < 3) return "Workspaces_Workspace_SlugTooShort";
  if (slug.length > 63) return "Workspaces_Workspace_SlugTooLong";
  if (RESERVED_SLUGS.has(slug)) return "Workspaces_Workspace_SlugReserved";
  return null;
}

/** What each store made for an `Idempotency-Key`: a retry answers with the same workspace. */
const createdUnderKey = new WeakMap<MockStore, Map<string, string>>();

/** The address the producer derives from a name: its words, plus a short suffix when that is unusable or taken. */
function deriveSlug(name: string, taken: (slug: string) => boolean): string {
  const base = name
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-|-$)/g, "")
    .slice(0, 55)
    .replace(/-$/, "");
  if (!slugViolation(base) && !taken(base)) return base;
  for (let attempt = 2; ; attempt += 1) {
    const candidate = `${base || "workspace"}-${attempt}`;
    if (!slugViolation(candidate) && !taken(candidate)) return candidate;
  }
}

const slugTaken = (store: MockStore) => (slug: string) =>
  isSlugTaken(
    slug,
    store.getWorkspaces().map((workspace) => workspace.slug),
  );

export const onboardingOperations = [
  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.slugCheck",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Workspaces.CheckWorkspaceSlug",
    },
    method: "GET",
    route: "/accounts/:accountId/workspaces/slug-check",
    async handle({ request, store }) {
      const slug = (request.searchParams.get("slug") ?? "")
        .trim()
        .toLowerCase();
      if (slugViolation(slug))
        return ok({ status: "invalid", reason: "invalid", suggestion: null });
      const taken = slugTaken(store);
      return taken(slug)
        ? ok({
            status: "taken",
            reason: "taken",
            suggestion: suggestSlug(slug, taken),
          })
        : ok({ status: "free", reason: null, suggestion: null });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { name?: string; slug?: string; size?: string; language?: string },
    unknown
  >({
    id: "onboarding.workspace.create",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Workspaces.CreateWorkspace",
    },
    method: "POST",
    route: "/accounts/:accountId/workspaces",
    async handle({ body, request, store }) {
      const key = request.headers.get("Idempotency-Key");
      if (!key)
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      const replay = createdUnderKey.get(store)?.get(key);
      if (replay) return created(replay);
      const name = (body?.name ?? "").trim();
      const chosen = (body?.slug ?? "").trim().toLowerCase();
      const slug = chosen || deriveSlug(name, slugTaken(store));
      const errors: Record<string, string[]> = {};
      if (!name) errors.name = ["Enter the company name."];
      else if (name.length > 160)
        errors.name = ["Use 160 characters or fewer for the name."];
      if (slugViolation(slug)) errors.slug = ["The address is not valid."];
      if (body?.size && !SIZE_BANDS.includes(body.size))
        errors.size = ["The company size is not one of the offered bands."];
      if (Object.keys(errors).length > 0) return validationError(errors);
      if (slugTaken(store)(slug)) return conflict("Address already taken");
      const { workspace } = store.createWorkspaceForCurrentUser({
        name,
        slug,
      });
      const made = createdUnderKey.get(store) ?? new Map<string, string>();
      made.set(key, workspace.id);
      createdUnderKey.set(store, made);
      return created(workspace.id);
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.draft.get",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Onboarding.GetOnboardingDraft",
    },
    method: "GET",
    route: "/workspaces/onboarding-draft",
    async handle({ store }) {
      const draft = store.onboarding.draftOf(store.getCurrentUser().id);
      return ok({
        draft: draft
          ? {
              ...draft,
              expiresAt: store.getClock().offsetSeconds(30 * 86_400),
            }
          : null,
      });
    },
  }),

  defineMockOperation<Record<string, never>, OnboardingDraft, unknown>({
    id: "onboarding.draft.save",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Onboarding.SaveOnboardingDraft",
    },
    method: "PUT",
    route: "/workspaces/onboarding-draft",
    async handle({ body, store }) {
      if (!body || typeof body.step !== "string" || !body.step)
        return validationError({ Step: ["Missing step."] });
      if (!body.data || typeof body.data !== "object")
        return validationError({ Data: ["The payload must be an object."] });
      store.onboarding.saveDraft(store.getCurrentUser().id, {
        step: body.step,
        workspaceId: body.workspaceId ?? null,
        data: body.data,
      });
      return noContent();
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.draft.clear",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Onboarding.ClearOnboardingDraft",
    },
    method: "DELETE",
    route: "/workspaces/onboarding-draft",
    async handle({ store }) {
      store.onboarding.clearDraft(store.getCurrentUser().id);
      return noContent();
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "onboarding.gettingStarted.get",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Onboarding.GetGettingStarted",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/getting-started",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const id = params.workspaceId;
      const boards = store.getBoards(id).filter((board) => !board.isArchived);
      // Derived: done once a Board the member can read has an Item.
      const withItem = boards.find((board) =>
        store.getLists(board.id).some((list) => store.getCards(list.id).length),
      );
      const newest = boards[boards.length - 1];
      const shown = withItem ?? newest;
      const dismissed = store.onboarding.isDismissed(id);
      return ok({
        dismissed,
        visible: !dismissed && !withItem,
        items: [
          {
            key: "first-item",
            done: Boolean(withItem),
            boardId: shown?.id ?? null,
            boardTitle: shown?.title ?? null,
          },
          {
            key: "invite-more",
            done: store.getInvitations(id).length > 0,
            boardId: null,
            boardTitle: null,
          },
        ],
      });
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "onboarding.gettingStarted.dismiss",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Onboarding.DismissGettingStarted",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/getting-started/dismiss",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      store.onboarding.dismiss(params.workspaceId);
      return noContent();
    },
  }),
];
