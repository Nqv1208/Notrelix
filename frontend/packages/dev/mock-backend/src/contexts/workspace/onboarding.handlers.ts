/**
 * Workspaces — the onboarding pages of the 2026-10 Onboarding mockup (gap
 * `CTR-GAP-ONBOARD-FLOW`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Onboarding"):
 *
 *   onboarding.slugCheck          — GET    /onboarding/slug-check?slug=
 *   onboarding.catalog            — GET    /onboarding/catalog?size=&name=
 *   onboarding.draft.get/save/clear — GET/PUT/DELETE /onboarding/draft
 *   onboarding.workspace.create   — POST   /onboarding/workspace
 *   onboarding.departments.create — POST   /workspaces/:id/onboarding/departments
 *   onboarding.invitations.send   — POST   /workspaces/:id/onboarding/invitations
 *   onboarding.board.create       — POST   /workspaces/:id/onboarding/board
 *   onboarding.gettingStarted.*   — GET /workspaces/:id/getting-started, POST .../dismiss, POST .../{key}/done
 *   onboarding.join.get/save      — GET/PUT /workspaces/:id/onboarding/join
 */
import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  BOARD_TEMPLATES,
  COMPANY_SIZES,
  DEPARTMENT_COLORS,
  SLUG_PATTERN,
  departmentSuggestions,
  isSlugTaken,
  suggestSlug,
  type CompanySize,
  type DepartmentColor,
  type GettingStartedKey,
  type OnboardingDraft,
} from "../../state/mock-onboarding";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-ONBOARD-FLOW" } as const;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const ROLES = ["Admin", "Member", "Viewer"] as const;

const slugTaken = (store: MockStore) => (slug: string) =>
  isSlugTaken(
    slug,
    store.getWorkspaces().map((workspace) => workspace.slug),
  );

function ownsWorkspace(store: MockStore, workspaceId: string): boolean {
  const role = store.getCurrentMemberRole(workspaceId);
  return role === "owner" || role === "admin";
}

const sizeOf = (value: string | null | undefined): CompanySize =>
  (COMPANY_SIZES as readonly string[]).includes(value ?? "")
    ? (value as CompanySize)
    : "m";

export const onboardingOperations = [
  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.slugCheck",
    contract: gap,
    method: "GET",
    route: "/onboarding/slug-check",
    async handle({ request, store }) {
      const slug = (request.searchParams.get("slug") ?? "")
        .trim()
        .toLowerCase();
      if (slug.length < 3 || slug.length > 30 || !SLUG_PATTERN.test(slug))
        return ok({ available: false, reason: "invalid", suggestion: null });
      const taken = slugTaken(store);
      return taken(slug)
        ? ok({
            available: false,
            reason: "taken",
            suggestion: suggestSlug(slug, taken),
          })
        : ok({ available: true, reason: null, suggestion: null });
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.catalog",
    contract: gap,
    method: "GET",
    route: "/onboarding/catalog",
    async handle({ request }) {
      const size = sizeOf(request.searchParams.get("size"));
      const suggestions = departmentSuggestions(
        size,
        request.searchParams.get("name") ?? "",
      );
      return ok({
        size,
        industry: suggestions.industry,
        departments: suggestions.departments,
        extras: suggestions.extras,
        colors: DEPARTMENT_COLORS,
        boardTemplates: BOARD_TEMPLATES,
      });
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.draft.get",
    contract: gap,
    method: "GET",
    route: "/onboarding/draft",
    async handle({ store }) {
      return ok({ draft: store.onboarding.draftOf(store.getCurrentUser().id) });
    },
  }),

  defineMockOperation<Record<string, never>, OnboardingDraft, unknown>({
    id: "onboarding.draft.save",
    contract: gap,
    method: "PUT",
    route: "/onboarding/draft",
    async handle({ body, store }) {
      if (!body || typeof body.step !== "string")
        return validationError({ step: ["Missing step."] });
      store.onboarding.saveDraft(store.getCurrentUser().id, {
        step: body.step,
        workspaceId: body.workspaceId ?? null,
        data: body.data ?? {},
      });
      return noContent();
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "onboarding.draft.clear",
    contract: gap,
    method: "DELETE",
    route: "/onboarding/draft",
    async handle({ store }) {
      store.onboarding.clearDraft(store.getCurrentUser().id);
      return noContent();
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { name?: string; slug?: string; size?: string; locale?: string },
    unknown
  >({
    id: "onboarding.workspace.create",
    contract: gap,
    method: "POST",
    route: "/onboarding/workspace",
    async handle({ body, store }) {
      const name = (body?.name ?? "").trim();
      const slug = (body?.slug ?? "").trim().toLowerCase();
      const errors: Record<string, string[]> = {};
      if (!name) errors.name = ["Enter the company name."];
      if (slug.length < 3 || slug.length > 30 || !SLUG_PATTERN.test(slug))
        errors.slug = ["Use 3 to 30 lowercase letters, digits and dashes."];
      if (Object.keys(errors).length > 0) return validationError(errors);
      if (slugTaken(store)(slug)) return conflict("Address already taken");
      const { workspace } = store.createWorkspaceForCurrentUser({
        name,
        slug,
      });
      return created({
        id: workspace.id,
        name: workspace.name,
        slug: workspace.slug,
        size: sizeOf(body?.size),
        locale: body?.locale === "en" ? "en" : "vi",
      });
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { departments?: { name?: string; color?: string }[] },
    unknown
  >({
    id: "onboarding.departments.create",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/onboarding/departments",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      if (!ownsWorkspace(store, params.workspaceId)) return forbidden();
      const list = body?.departments ?? [];
      const names = list.map((entry) => (entry.name ?? "").trim());
      if (names.length === 0 || names.some((name) => !name))
        return validationError({ departments: ["Name every department."] });
      if (
        new Set(names.map((name) => name.toLowerCase())).size !== names.length
      )
        return validationError({
          departments: ["Two departments share a name."],
        });
      const result: { id: string; name: string; color: DepartmentColor }[] = [];
      for (const [index, entry] of list.entries()) {
        const id = store.nextId("space");
        const color = (DEPARTMENT_COLORS as readonly string[]).includes(
          entry.color ?? "",
        )
          ? (entry.color as DepartmentColor)
          : (DEPARTMENT_COLORS[
              index % DEPARTMENT_COLORS.length
            ] as DepartmentColor);
        const write = store.spaces.create(
          params.workspaceId,
          store.spaceViewer(params.workspaceId),
          {
            id,
            name: names[index] ?? "",
            type: "Department",
            visibility: "Private",
            description: null,
            parentId: null,
            previousSiblingId: null,
            nextSiblingId: null,
            now: store.getClock().isoNow(),
          },
        );
        if (write.kind !== "ok")
          return validationError({
            departments: ["Could not create a department."],
          });
        const space = store.spaces.getSpace(id);
        if (space) space.color = color;
        result.push({ id, name: names[index] ?? "", color });
      }
      return created({ departments: result });
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { invites?: { email?: string; spaceId?: string; role?: string }[] },
    unknown
  >({
    id: "onboarding.invitations.send",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/onboarding/invitations",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      if (!ownsWorkspace(store, params.workspaceId)) return forbidden();
      const invites = body?.invites ?? [];
      const errors: Record<string, string[]> = {};
      invites.forEach((invite, index) => {
        if (!EMAIL.test((invite.email ?? "").trim()))
          errors[`invites[${index}].email`] = ["Email is not a valid address."];
        if (!(ROLES as readonly string[]).includes(invite.role ?? ""))
          errors[`invites[${index}].role`] = ["Unknown role."];
      });
      if (Object.keys(errors).length > 0) return validationError(errors);
      let sent = 0;
      const skipped: string[] = [];
      for (const invite of invites) {
        // They join the workspace as a member; the department and the role in
        // it apply when they accept.
        const id = store.createInvitation(
          params.workspaceId,
          invite.email ?? "",
          "member",
        );
        if (id) sent += 1;
        else skipped.push((invite.email ?? "").trim().toLowerCase());
      }
      store.onboarding.startList(
        params.workspaceId,
        store.onboarding.invitesOf(params.workspaceId) + sent,
      );
      return ok({ sent, skipped });
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { templateId?: string; name?: string; spaceId?: string | null },
    unknown
  >({
    id: "onboarding.board.create",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/onboarding/board",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      if (!ownsWorkspace(store, params.workspaceId)) return forbidden();
      const template = BOARD_TEMPLATES.find(
        (entry) => entry.id === body?.templateId,
      );
      const name = (body?.name ?? "").trim();
      const errors: Record<string, string[]> = {};
      if (!template) errors.templateId = ["Choose a template."];
      if (!name) errors.name = ["Name the board."];
      if (body?.spaceId && !store.spaces.getSpace(body.spaceId))
        errors.spaceId = ["Unknown department."];
      if (Object.keys(errors).length > 0) return validationError(errors);
      const board = store.createBoard(params.workspaceId, {
        title: name,
        spaceId: body?.spaceId ?? null,
      });
      store.onboarding.startList(
        params.workspaceId,
        store.onboarding.invitesOf(params.workspaceId),
      );
      return created({ id: board.id, templateId: template?.id ?? "blank" });
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "onboarding.gettingStarted.get",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/getting-started",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const id = params.workspaceId;
      const visible =
        store.onboarding.hasList(id) && !store.onboarding.isDismissed(id);
      const boards = store.getBoards(id).filter((board) => !board.isArchived);
      const first = boards[boards.length - 1];
      const invites = store.onboarding.invitesOf(id);
      const keys: GettingStartedKey[] = ["first-item", "invite-more", "mobile"];
      return ok({
        visible,
        items: keys.map((key) => ({
          key,
          done: store.onboarding.isDone(id, key),
          boardId: key === "first-item" ? (first?.id ?? null) : null,
          boardTitle: key === "first-item" ? (first?.title ?? null) : null,
          pendingInvites: key === "invite-more" ? invites : null,
        })),
      });
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "onboarding.gettingStarted.dismiss",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/getting-started/dismiss",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      store.onboarding.dismiss(params.workspaceId);
      return noContent();
    },
  }),

  defineMockOperation<{ workspaceId: string; key: string }, never, unknown>({
    id: "onboarding.gettingStarted.done",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/getting-started/:key/done",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const key = params.key as GettingStartedKey;
      if (!["first-item", "invite-more", "mobile"].includes(key))
        return notFound();
      store.onboarding.markDone(params.workspaceId, key);
      return noContent();
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "onboarding.join.get",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/onboarding/join",
    async handle({ params, store }) {
      const workspace = store.getWorkspace(params.workspaceId);
      if (!workspace || !store.isCurrentMemberOf(params.workspaceId))
        return notFound();
      const user = store.getCurrentUser();
      const viewer = store.spaceViewer(params.workspaceId);
      const spaces = store.spaces
        .spacesOf(params.workspaceId)
        .filter(
          (space) =>
            !space.archived &&
            !space.parentId &&
            store.spaces.accessFor(space.id, viewer) !== null,
        )
        .map((space) => {
          const boards = store
            .getBoards(params.workspaceId)
            .filter((board) => board.spaceId === space.id && !board.isArchived);
          return {
            id: space.id,
            name: space.name,
            color: space.color ?? null,
            open: space.visibility === "Workspace",
            access: store.spaces.accessFor(space.id, viewer),
            boards: boards.map((board) => ({
              id: board.id,
              title: board.title,
              level: "editor" as const,
            })),
          };
        });
      const profile = store.onboarding.profileOf(
        `${params.workspaceId}:${user.id}`,
      );
      return ok({
        workspace: {
          id: workspace.id,
          name: workspace.name,
          memberCount: store.getWorkspaceMembers(workspace.id).length,
        },
        inviterName: "Nguyễn Thu Hà",
        role: store.getCurrentMemberRole(workspace.id) ?? "member",
        spaces,
        profile: {
          displayName: profile?.displayName ?? user.name,
          notifyEmail: profile?.notifyEmail ?? "digest",
        },
        companyAccent: "blue",
      });
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { displayName?: string; notifyEmail?: string },
    unknown
  >({
    id: "onboarding.join.save",
    contract: gap,
    method: "PUT",
    route: "/workspaces/:workspaceId/onboarding/join",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const displayName = (body?.displayName ?? "").trim();
      if (!displayName)
        return validationError({ displayName: ["Enter your name."] });
      store.onboarding.saveProfile(
        `${params.workspaceId}:${store.getCurrentUser().id}`,
        {
          displayName,
          notifyEmail: body?.notifyEmail === "mentions" ? "mentions" : "digest",
        },
      );
      return noContent();
    },
  }),
];
